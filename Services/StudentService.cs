using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using EDS.Models;
using System.Web.Mvc;
using System.Data.Entity;
using EDS.Constants;

namespace EDS.Services
{
    public class StudentService
    {
        private SiteUser _siteUser;
        private dbTIREntities _db;

        public StudentService(SiteUser siteUser, dbTIREntities db)
        {
            _db = db;
            _siteUser = siteUser;
        }

        public List<Student> GetViewData(string hiddenSchoolYearFilter, string hiddenSchoolFilter)
        {
            return GetViewData(hiddenSchoolYearFilter, hiddenSchoolFilter, "");
        }

        public List<Student> GetViewData(string schoolYearFilter, string schoolFilter, string searchFilter)
        {
            
            int siteSchoolYear = Convert.ToInt32(schoolYearFilter);
            int schoolYearId = _db.tblSchoolYears.Where(s => s.SchoolYear == siteSchoolYear).Select(s => s.SchoolYearId).FirstOrDefault();

            IQueryable<Student> query;

            if (_siteUser.isTeacher)
            {
                query = (
                    from classTeacher in _db.tblClassTeachers
                    join classStudent in _db.tblClassStudents on classTeacher.ClassId equals classStudent.ClassId
                    join student in _db.tblStudents on classStudent.StudentId equals student.StudentId
                    join studentSchoolYear in _db.tblStudentSchoolYears on student.StudentId equals studentSchoolYear.StudentId
                    join schoolClass in _db.tblClasses on classStudent.ClassId equals schoolClass.ClassId
                    join school in _db.tblSchools on schoolClass.SchoolId equals school.SchoolId
                    join schoolYear in _db.tblSchoolYears on student.SchoolYearId equals schoolYear.SchoolYearId
                    where classTeacher.UserId == _siteUser.EdsUserId
                    && schoolYear.SchoolYear == siteSchoolYear
                    && studentSchoolYear.SchoolYearId == schoolYearId
                    && studentSchoolYear.ServingSchoolId == school.SchoolId
                    select new Student()
                    {
                        StudentId = student.StudentId,
                        FirstName = student.FirstName,
                        LastName = student.LastName,
                        StateId = studentSchoolYear.StateId,
                        LocalId = studentSchoolYear.LocalId,
                        GradeLevel = studentSchoolYear.GradeLevel,
                        HomeSchool = school.SchoolDesc,
                        TeacherId = classTeacher.UserId,
                        SchoolId = school.SchoolId
                    });
            }
            else
            {
                int userAssignedDistrict = _siteUser.Districts[0].Id;
                query = (
                    from userSchool in _db.tblUserSchools
                    join school in _db.tblSchools on userSchool.SchoolId equals school.SchoolId
                    join studentSchoolYear in _db.tblStudentSchoolYears on school.SchoolId equals studentSchoolYear.ServingSchoolId
                    join student in _db.tblStudents on studentSchoolYear.StudentId equals student.StudentId
                    where userSchool.UserId == _siteUser.EdsUserId
                    && userSchool.SchoolYearId == schoolYearId
                    && studentSchoolYear.SchoolYearId == schoolYearId
                    select new Student()
                    {
                        StudentId = student.StudentId,
                        FirstName = student.FirstName,
                        LastName = student.LastName,
                        StateId = studentSchoolYear.StateId,
                        LocalId = studentSchoolYear.LocalId,
                        GradeLevel = studentSchoolYear.GradeLevel,
                        HomeSchool = school.SchoolDesc,
                        SchoolId = school.SchoolId
                    });
            }

            // Filter by UI
            if (!String.IsNullOrEmpty(schoolFilter) && schoolFilter != "-1")
            {
                var schoolsForUser = HelperService.GetUserSchoolsFilter(_siteUser.EdsUserId, _siteUser.Districts[0].Id, Convert.ToInt32(schoolFilter), _db);
                query = query.Where(x => schoolsForUser.Contains(x.SchoolId));
            }

            if (!String.IsNullOrEmpty(searchFilter))
            {
                query = query.Where(x => x.FirstName.ToUpper().Contains(searchFilter.ToUpper()) || x.LastName.ToUpper().Contains(searchFilter.ToUpper()));
            }

            return query.AsEnumerable()
                        .Distinct(new EdsUserIdStudentComparer())
                        .ToList();
        }

        public StudentExt GetStudentDetail(int studentId, int schoolYear)
        {
            IQueryable<StudentExt> query;

            query = (
                        from student in _db.tblStudents
                        join studentSchoolYear in _db.tblStudentSchoolYears on student.StudentId equals studentSchoolYear.StudentId
                        join schoolYears in _db.tblSchoolYears on studentSchoolYear.SchoolYearId equals schoolYears.SchoolYearId
                        where student.StudentId == studentId && schoolYears.SchoolYear == schoolYear
                        select new StudentExt()
                        {
                            StudentId = student.StudentId,
                            FirstName = student.FirstName,
                            MiddleName = student.MiddleName,
                            LastName = student.LastName,
                            LineageId = student.LineageId,
                            RaceId = student.RaceId,
                            GenderId = student.GenderId,
                            BirthDate = student.BirthDate,
                            HomeLanguageId = student.HomeLanguageId,
                            NativeLanguageId = student.NativeLanguageId,
                            GradeLevel = studentSchoolYear.GradeLevel,
                            StateId = studentSchoolYear.StateId,
                            LocalId = studentSchoolYear.LocalId,
                            StudentSchoolYearId = studentSchoolYear.StudentSchoolYearId,
                            SchoolYearId = studentSchoolYear.SchoolYearId,
                            ServingSchoolId = studentSchoolYear.ServingSchoolId,
                            LepIndicator = studentSchoolYear.LepIndicator,
                            IepIndicator = studentSchoolYear.IepIndicator,
                            FrlIndicator = studentSchoolYear.FrlIndicator,
                            Hispanic = studentSchoolYear.Hispanic,
                            EnrollmentDate = studentSchoolYear.EnrollmentDate,
                            CreateDateTime = studentSchoolYear.CreateDateTime,
                            SchoolYearDesc = schoolYears.SchoolYearDesc,
                            SchoolYear = schoolYear
                        }
                    );
            return query.AsEnumerable().FirstOrDefault();
        }

        /// <summary>
        /// Pulls all rows from tblLineage and wraps results
        /// as MultiSelectList. 
        /// </summary>
        public List<DropDownIdDesc> DropDownDataForLineage()
        {
            IQueryable<DropDownIdDesc> query = (from lineage in _db.tblLineages
                                                select new DropDownIdDesc()
                                                {
                                                    Id = lineage.LineageId,
                                                    Desc = lineage.LineageDesc
                                                });
            return query.OrderBy(q => q.Desc).ToList();
        }

      
        /// <summary>
        /// Pulls all rows from tblLanguage and wraps results
        /// as MultiSelectList. 
        /// </summary>
        public List<DropDownIdDesc> DropDownDataForLanguage()
        {
            IQueryable<DropDownIdDesc> query = (from language in _db.tblLanguages
                                                select new DropDownIdDesc()
                                                {
                                                    Id = language.LanguageId,
                                                    Desc = language.LanguageDesc
                                                });
            return query.OrderBy(q => q.Desc).ToList();
        }

        /// <summary>
        /// Pulls all rows from List and wraps results
        /// as MultiSelectList. 
        /// </summary>
        public List<DropDownIdDesc> DropDownDataForGrade()
        {
            IQueryable<DropDownIdDesc> query = (from grade in SystemParameter.gradeDict.AsQueryable()
                                                select new DropDownIdDesc()
                                                {
                                                    Id = grade.Key,
                                                    Desc = grade.Value
                                                });
            return query.OrderBy(q => q.Id).ToList();
        }


        /// <summary>
        /// This function is used to check if another student already exists with same localId and stateId
        /// </summary>
        /// <param name="stateId"></param>
        /// <param name="localId"></param>
        /// <param name="studentId"></param>
        /// <returns></returns>
        public bool IsStudentExist(string stateId, string localId, int? studentId = null)
        {
            bool isStudentExist = false;
            IQueryable<tblStudentSchoolYear> query = null;
            try
            {
                if (stateId != null || localId != null)
                {
                    query = stateId != null ? _db.tblStudentSchoolYears.Where(x => x.StateId == stateId) : _db.tblStudentSchoolYears.Where(x => x.LocalId == localId);
                    if (studentId != null)
                    {
                        query = query.Where(x => x.StudentId != studentId);
                    }
                    isStudentExist = query.Count() > 0 ? true : false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ERROR: {0}", ex));
            }
            return isStudentExist;
        }

        /// <summary>
        /// This function is used to Add and Update student
        /// </summary>
        /// <param name="studentExtend"></param>
        public void SaveStudents(StudentExt studentExtend)
        {
            using (var context = new dbTIREntities())
            {
                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        tblStudent newtblStudent = new tblStudent()
                        {
                            FirstName = studentExtend.FirstName,
                            MiddleName = studentExtend.MiddleName,
                            LastName = studentExtend.LastName,
                            DistrictId = studentExtend.DistrictId,
                            StateId = studentExtend.StateId,
                            LocalId = studentExtend.LocalId,
                            LineageId = studentExtend.LineageId == -1 ? null : studentExtend.LineageId,
                            RaceId = studentExtend.RaceId == -1 ? null : studentExtend.RaceId,
                            GenderId = studentExtend.GenderId,
                            BirthDate = studentExtend.BirthDate,
                            HomeLanguageId = studentExtend.HomeLanguageId == -1 ? null : studentExtend.HomeLanguageId,
                            NativeLanguageId = studentExtend.NativeLanguageId == -1 ? null : studentExtend.NativeLanguageId,
                            ChangeDatetime = DateTime.Now
                        };
                        if (studentExtend.StudentId == 0)
                        {
                            context.tblStudents.Add(newtblStudent);
                        }
                        else
                        {
                            newtblStudent.StudentId = studentExtend.StudentId;
                            context.tblStudents.Add(newtblStudent);
                            context.Entry(newtblStudent).State = EntityState.Modified;
                        }
                        context.SaveChanges();
                        tblStudentSchoolYear newtblStudentSchoolYear = new tblStudentSchoolYear()
                        {
                            StudentId = newtblStudent.StudentId,
                            SchoolYearId = studentExtend.SchoolYearId,
                            ServingSchoolId = studentExtend.ServingSchoolId,
                            GradeLevel = studentExtend.GradeLevel,
                            StateId = studentExtend.StateId,
                            LocalId = studentExtend.LocalId,
                            LepIndicator = studentExtend.LepIndicator,
                            IepIndicator = studentExtend.IepIndicator,
                            FrlIndicator = studentExtend.FrlIndicator,
                            EnrollmentDate = studentExtend.EnrollmentDate,
                            ChangeDateTIme = DateTime.Now,
                            Hispanic = studentExtend.Hispanic
                        };

                        if (studentExtend.StudentSchoolYearId == 0)
                        {
                            context.tblStudentSchoolYears.Add(newtblStudentSchoolYear);
                        }
                        else
                        {
                            newtblStudentSchoolYear.StudentSchoolYearId = studentExtend.StudentSchoolYearId;
                            context.tblStudentSchoolYears.Add(newtblStudentSchoolYear);
                            context.Entry(newtblStudentSchoolYear).State = EntityState.Modified;
                        }

                        context.SaveChanges();
                        dbContextTransaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback();
                        throw ex;
                    }
                }
            }

        }

    }
}