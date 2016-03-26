using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using EDS.Models;
using System.Web.Mvc;
using EntityFramework.Extensions;
using System.Data.Entity;

namespace EDS.Services
{
    public class ClassService
    {
        private SiteUser _siteUser;
        private dbTIREntities _db;

        public ClassService(SiteUser siteUser, dbTIREntities db)
        {
            _db = db;
            _siteUser = siteUser;
        }

        public List<SchoolClass> GetViewData(string hiddenSchoolYearFilter, string hiddenSchoolFilter)
        {
            return GetViewData(hiddenSchoolYearFilter, hiddenSchoolFilter, "");
        }

        public List<SchoolClass> GetViewData(string schoolYearFilter, string schoolFilter, string searchFilter)
        {
            int userAssignedDistrict = _siteUser.Districts[0].Id;
            int siteSchoolYear = Convert.ToInt32(schoolYearFilter);

            IQueryable<SchoolClass> query = (
                from schoolClass in _db.tblClasses
                join schoolYear in _db.tblSchoolYears on schoolClass.SchoolYearId equals schoolYear.SchoolYearId
                join school in _db.tblSchools on schoolClass.SchoolId equals school.SchoolId

                // Filter by school year
                where schoolYear.SchoolYear == siteSchoolYear
                    // Filter by user's district
                && school.DistrictId == userAssignedDistrict

                select new SchoolClass()
                {
                    SchoolClassId = schoolClass.ClassId,
                    ClassDesc = schoolClass.ClassDesc,
                    SchoolYearDesc = schoolYear.SchoolYearDesc,
                    SchoolDesc = school.SchoolDesc,
                    DistrictId = school.DistrictId,
                    SchoolId = school.SchoolId,
                    TeachersCount = schoolClass.tblClassTeachers.Count,
                    StudentsCount = schoolClass.tblClassStudents.Count,
                });
           
            // Filter by School
            int intTryParseResult;
            if (int.TryParse(schoolFilter, out intTryParseResult) && intTryParseResult != -1)
            {
                query = query.Where(x => x.SchoolId == intTryParseResult);
            }
            else
            {
                if (_siteUser.isAdministrator || _siteUser.isDataAdministrator)
                {
                    int schoolYearId = _db.tblSchoolYears.Where(x => x.SchoolYear == siteSchoolYear).Select(x => x.SchoolYearId).FirstOrDefault();
                    int[] schoolsForUser = _siteUser.Schools.Where(x => x.SchoolYearId == schoolYearId).Select(x => x.Id).ToArray();
                    query = query.Where(x => schoolsForUser.Contains(x.SchoolId));
                    if (schoolsForUser.Count() == 0)
                    {
                        query = query.Where(x => x.SchoolId == intTryParseResult);
                    }
                }
            }
            if (!String.IsNullOrEmpty(searchFilter))
            {
                query = query.Where(x => x.ClassDesc.ToUpper().Contains(searchFilter.ToUpper()));
            }

            return query.OrderBy(x => x.ClassDesc).ToList();
        }

        public string UpdateStudents(int classId, List<string> addedStudentIds, List<string> removedStudentIds)
        {
            string result = "Success";
            using (var context = new dbTIREntities())
            {
                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Delete selected students for this class
                        if (removedStudentIds != null)
                        {
                            foreach (var student in removedStudentIds)
                            {
                                int studentId = Convert.ToInt32(student);
                                context.tblClassStudents.Delete(x => x.ClassId == classId && x.StudentId == studentId);
                            }
                        }

                        if (addedStudentIds != null)
                        {
                            // 2. Add selected students for this class
                            foreach (var student in addedStudentIds)
                            {
                                context.tblClassStudents.Add(new tblClassStudent()
                                {
                                    ClassId = Convert.ToInt32(classId),
                                    StudentId = Convert.ToInt32(student),
                                    CreateDatetime = DateTime.Now
                                });
                                context.SaveChanges();
                            }
                        }

                        // 3. Commit changes
                        dbContextTransaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // 4. Rollback changes
                        dbContextTransaction.Rollback();
                        result = String.Format("Error: An error occurred in UpdateStudentsInClass() while adding/removing students from this class. Error: {0}", ex.ToString());
                    }
                }
            }
            return result;
        }

        public string UpdateTeachers(int classId, List<string> teacherId)
        {
            string result = "Success";

            using (var context = new dbTIREntities())
            {
                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Delete all teachers for this class
                        context.tblClassTeachers.Delete(x => x.ClassId == classId);

                        // 2. Add all teachers
                        if (teacherId != null)
                        {
                            foreach (var teacher in teacherId)
                            {
                                context.tblClassTeachers.Add(new tblClassTeacher()
                                {
                                    ClassId = Convert.ToInt32(classId),
                                    UserId = Convert.ToInt32(teacher),
                                    CreateDatetime = DateTime.Now
                                });
                                context.SaveChanges();
                            }
                        }

                        // 3. Commit changes
                        dbContextTransaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // 4. Rollback changes
                        dbContextTransaction.Rollback();
                        result = String.Format("Error: An error occurred in UpdateTeachersInClass() while adding/removing teachers from this class. Error: {0}", ex.ToString());
                    }
                }
            }
            return result;
        }

        public void AddClass(tblClass newClass)
        {
            newClass.CreateDatetime = DateTime.Now;
            _db.tblClasses.Add(newClass);
            _db.SaveChanges();
        }

        internal void EditClass(tblClass editClass)
        {
            editClass.ChangeDatetime = DateTime.Now;
            _db.Entry(editClass).State = EntityState.Modified;
            _db.SaveChanges();
        }

        public bool IsClassExists(tblClass newClass)
        {
            int userAssignedDistrict = _siteUser.Districts[0].Id;
            var count = (from x in _db.tblClasses
                         join y in _db.tblSchools on x.SchoolId equals y.SchoolId
                         where x.SchoolYearId == newClass.SchoolYearId && x.ClassDesc == newClass.ClassDesc
                               && y.DistrictId == userAssignedDistrict
                         select x.ClassId).Count();

            return (count > 0 ? true : false);
        }

        public bool IsEditClassExists(tblClass editClass)
        {
            int userAssignedDistrict = _siteUser.Districts[0].Id;
            var count = (from x in _db.tblClasses
                         join y in _db.tblSchools on x.SchoolId equals y.SchoolId
                         where x.SchoolYearId == editClass.SchoolYearId && x.ClassDesc == editClass.ClassDesc
                               && x.ClassId != editClass.ClassId && y.DistrictId == userAssignedDistrict
                         select x).Count();

            return (count > 0 ? true : false);
        }




        /// <summary>
        /// DeleteClass() used to delete a class.
        /// </summary>
        /// <param name="classId"></param>
        /// <returns></returns>
        public string DeleteClass(int classId)
        {
            string result = "Success";
            using (var context = new dbTIREntities())
            {
                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    try
                    {

                        context.tblClassStudents.Where(x => x.ClassId == classId).Delete();
                        context.tblClassTeachers.Where(x => x.ClassId == classId).Delete(); ;
                        context.tblClasses.Where(x => x.ClassId == classId).Delete(); ;

                        context.SaveChanges();

                        dbContextTransaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback();
                        result = String.Format("Error: An error occurred in DeleteClass() while deleting class. Error: {0}", ex.ToString());
                    }
                }
            }
            return result;
        }



        /// <summary>
        /// GetTeachersForThisClass method get the teachers for a class
        /// </summary>
        /// <param name="classId"></param>
        /// <returns></returns>

        public IQueryable<TeacherDetail> GetTeachersForThisClass(tblClass tblClass)
        {
            var query = GetTeacherDetail(tblClass.ClassId);
            return query.Distinct().OrderBy(x => x.LastName);
        }

        /// <summary>
        /// GetTeachersForThisClass method get all teachers except a class
        /// </summary>
        /// <param name="classId"></param>
        /// <returns></returns>

        public IQueryable<TeacherDetail> GetTeachersNotForThisClass(tblClass tblClass, bool filterTeachersByDistrict, int districtId)
        {
            var teachersForThisClass = GetTeacherDetail(tblClass.ClassId);
            var query = _db.tblUsers
                         .Join(_db.tblUserSchools, u => u.UserId, us => us.UserId, (u, us) => new { u, us })
                         .Join(_db.tblSchools, uus => uus.us.SchoolId, s => s.SchoolId, (uus, s) => new { uus, s })
                         .Join(_db.tblSchoolYears, uussy => uussy.uus.us.SchoolYearId, sy => sy.SchoolYearId, (uussy, sy) => new { uussy, sy })
                         .Join(_db.tblUserDistricts, uussyud => uussyud.uussy.uus.u.UserId, ud => ud.UserId, (uussuud, ud) => new { uussuud, ud })
                         .Where(x => x.ud.DistrictId == districtId
                                     && (filterTeachersByDistrict || x.uussuud.uussy.s.SchoolId == tblClass.SchoolId) && x.uussuud.sy.SchoolYearId == tblClass.SchoolYearId)
                         .Select(x => new TeacherDetail() { TeacherId = x.uussuud.uussy.uus.u.UserId, FullName = x.uussuud.uussy.uus.u.LastName + ", " + x.uussuud.uussy.uus.u.FirstName, LastName = x.uussuud.uussy.uus.u.LastName })
                         .Except(teachersForThisClass)
                         .Distinct()
                         .OrderBy(x => x.LastName);

            return query;
        }


        /// <summary>
        /// GetStudentForThisClass method get all student for a class.
        /// </summary>
        /// <param name="classId"></param>
        /// <returns></returns>
        public IQueryable<StudentDetail> GetStudentForThisClass(tblClass tblClass)
        {
            var query = GetStudentDetail(tblClass.ClassId);
            return query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.MiddleName);

        }

        /// <summary>
        /// GetStudentNotForThisClass method get all student for all classes except a class.
        /// </summary>
        /// <param name="classId"></param>
        /// <param name="userAssignedDistrict"></param>
        /// <param name="schoolYearId"></param>
        /// <returns></returns>
        public IQueryable<StudentDetail> GetStudentNotForThisClass(tblClass tblClass, int districtId)
        {
            var studentsForThisClass = GetStudentDetail(tblClass.ClassId);

            var query = _db.tblStudents.Join(_db.tblStudentSchoolYears, s => s.StudentId, rc => rc.StudentId, (s, rc) => new { s, rc })
                   .Where(x => x.s.DistrictId == districtId && x.rc.SchoolYearId == tblClass.SchoolYearId)
                   .Select(x => new StudentDetail()
                   {
                       StudentId = x.s.StudentId,
                       FullName = (x.s.LastName ?? string.Empty) + ", " + (x.s.FirstName ?? string.Empty) + " " + (x.s.MiddleName ?? string.Empty)
                       ,
                       LastName = x.s.LastName,
                       FirstName = x.s.FirstName,
                       MiddleName = x.s.MiddleName
                   })
                   .Except(studentsForThisClass)
                   .OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.MiddleName);

            return query;
        }

        /// <summary>
        /// GetStudentForSchool method get students for a school except a class.
        /// </summary>
        /// <param name="classId"></param>
        /// <param name="schoolId"></param>
        /// <param name="userAssignedDistrict"></param>
        /// <param name="schoolYearId"></param>
        /// <returns></returns>
        public IQueryable<StudentDetail> GetStudentForSchool(tblClass tblClass, int districtId)
        {
            var studentsForThisClass = GetStudentDetail(tblClass.ClassId);

            var query = _db.tblStudents
                                      .Join(_db.tblStudentSchoolYears, s => s.StudentId, rc => rc.StudentId, (s, rc) => new { s, rc })
                                      .Where(y => y.s.DistrictId == districtId && y.rc.ServingSchoolId == tblClass.SchoolId && y.rc.SchoolYearId == tblClass.SchoolYearId)
                                      .Select(y => new StudentDetail()
                                      {
                                          StudentId = y.s.StudentId,
                                          FullName = (y.s.LastName ?? string.Empty) + ", " + (y.s.FirstName ?? string.Empty) + " " + (y.s.MiddleName ?? string.Empty)
                                          ,
                                          LastName = y.s.LastName,
                                          FirstName = y.s.FirstName,
                                          MiddleName = y.s.MiddleName
                                      })
                                      .Except(studentsForThisClass)
                                      .OrderBy(records => records.LastName).ThenBy(records => records.FirstName).ThenBy(records => records.MiddleName);

            return query;

        }

        private IQueryable<TeacherDetail> GetTeacherDetail(int classId)
        {
            return (_db.tblUsers
                    .Join(_db.tblClassTeachers, u => u.UserId, ct => ct.UserId, (u, ct) => new { u, ct })
                    .Where(x => x.ct.ClassId == classId)
                    .Select(x => new TeacherDetail() { TeacherId = x.u.UserId, FullName = x.u.LastName + ", " + x.u.FirstName, LastName = x.u.LastName }));
        }

        private IQueryable<StudentDetail> GetStudentDetail(int classId)
        {
            return (_db.tblStudents.Join(_db.tblClassStudents, s => s.StudentId, cs => cs.StudentId, (s, cs) => new { s, cs })
                                    .Where(t => t.cs.ClassId == classId)
                                    .Select(t => new StudentDetail()
                                    {
                                        StudentId = t.s.StudentId,
                                        FullName = (t.s.LastName ?? string.Empty) + ", " + (t.s.FirstName ?? string.Empty) + " " + (t.s.MiddleName ?? string.Empty)
                                        ,
                                        LastName = t.s.LastName,
                                        FirstName = t.s.FirstName,
                                        MiddleName = t.s.MiddleName
                                    }));

        }
    }
}