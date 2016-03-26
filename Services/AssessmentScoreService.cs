using System.Data.Common;
using System.Data.Entity;
using System.Web.UI.WebControls;
using EDS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EDS.Services
{

    public class AssessmentScoreService
    {
        private SiteUser _siteUser;
        private dbTIREntities _db;

        public AssessmentScoreService(SiteUser siteUser, dbTIREntities db)
        {
            _db = db;
            _siteUser = siteUser;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public List<StudentScore> GetAssessmentScoreData(AssessmentScoreViewModel model)
        {
            
            int schoolYearId = int.Parse(model.SchoolYearId);
            int assessmentTypeId = int.Parse(model.AssessmentTypeId);
            int schoolTermId = int.Parse(model.SchoolTermId);
            List<int> schoolIds = _db.tblSchools.Where(x=>x.DistrictId == model.DistrictId).Select(x=>x.SchoolId).ToList(); 

            IQueryable<StudentScore> query;

                int userAssignedDistrict = _siteUser.Districts[0].Id;
                query = (
                    from a in _db.tblAssessments
                    join ass in _db.tblAssessmentScores on a.AssessmentId equals ass.AssessmentId
                    join s in _db.tblStudents on ass.StudentId equals s.StudentId
                    join ssy in _db.tblStudentSchoolYears on s.StudentId equals ssy.StudentId
                    join sub in _db.tblSubjects on a.SubjectId equals sub.SubjectId
                    where a.AssessmentTypeId == assessmentTypeId
                    && a.SchoolTermId == schoolTermId
                    && a.SchoolYearId == schoolYearId
                    && ssy.SchoolYearId == schoolYearId
                    && schoolIds.Contains(ssy.ServingSchoolId)                     
                    select new StudentScore()
                    {
                        Subject = sub.SubjectDesc,
                        LocalId = s.LocalId,
                       FirstName = s.FirstName,
                       LastName = s.LastName,
                       Score = ass.Score,
                       AssessmentType = a.tblAssessmentType.AssessmentTypeDesc,
                       SchoolTerm = a.tblSchoolTerm.SchoolTermDesc,
                       SchoolYear = ssy.tblSchoolYear.SchoolYearDesc,
                       AssessmentDesc = a.AssessmentDesc,
                       StudentId = s.StudentId
                    });

                var result = query.Where(s => (s.FirstName.ToUpper() + " " + s.LastName.ToUpper()).Contains(model.StudentName.ToUpper())).ToList(); 
                return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="studentId"></param>
        /// <param name="schoolYear"></param>
        /// <param name="schoolTerm"></param>
        /// <param name="assessmentType"></param>
        /// <param name="assessmentDesc"></param>
        /// <param name="localId"></param>
        /// <param name="subject"></param>
        /// <returns></returns>
        public List<StudentScore> GetStudentAssessmentScore(int studentId, string schoolYear, string schoolTerm, string assessmentType, string assessmentDesc, string localId, string subject)
        {

            IQueryable<StudentScore> query;

            query = (
                from a in _db.tblAssessments
                join ass in _db.tblAssessmentScores on a.AssessmentId equals ass.AssessmentId
                join s in _db.tblStudents on ass.StudentId equals s.StudentId
                join ssy in _db.tblStudentSchoolYears on s.StudentId equals ssy.StudentId
                join sub in _db.tblSubjects on a.SubjectId equals sub.SubjectId
                where s.StudentId== studentId && ssy.StudentId==studentId
                && schoolYear == ssy.tblSchoolYear.SchoolYearDesc
                && schoolTerm == a.tblSchoolTerm.SchoolTermDesc
                && assessmentType == a.tblAssessmentType.AssessmentTypeDesc
                && assessmentDesc == a.AssessmentDesc
                && localId==s.LocalId
                && subject == sub.SubjectDesc
                select new StudentScore()
                {
                    Subject = sub.SubjectDesc,
                    LocalId = s.LocalId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Score = ass.Score,
                    AssessmentType = a.tblAssessmentType.AssessmentTypeDesc,
                    SchoolTerm = a.tblSchoolTerm.SchoolTermDesc,
                    SchoolYear = ssy.tblSchoolYear.SchoolYearDesc,
                    AssessmentDesc = a.AssessmentDesc,
                    StudentId = s.StudentId,
                    AssessmentId = a.AssessmentId
                });

            var result = query.Where(s => (s.StudentId == studentId 
                && schoolYear == s.SchoolYear
                && schoolTerm == s.SchoolTerm
                && assessmentType == s.AssessmentType 
                && assessmentDesc == s.AssessmentDesc 
                && localId == s.LocalId
                && subject == s.Subject
                )).ToList();
          
            return result;
        }



        /// <summary>
        /// This function is used to Add and Update student
        /// </summary>
        /// <param name="assessmentScore"></param>
        public void UpdateStudentScore(tblAssessmentScore assessmentScore )
        {

            using (var dbContextTransaction = _db.Database.BeginTransaction())
                {
                    try
                    {
                        
                        assessmentScore.ChangeDatetime = DateTime.Now;

                        _db.tblAssessmentScores.Attach(assessmentScore);
                        _db.Entry(assessmentScore).State = EntityState.Modified;

                        _db.SaveChanges();
                       
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