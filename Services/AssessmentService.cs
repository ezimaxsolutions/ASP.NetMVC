using EDS.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EDS.Services
{
    public class AssessmentService
    {
        private SiteUser _siteUser;
        private dbTIREntities _db;

        public AssessmentService(SiteUser siteUser, dbTIREntities db)
        {
            _db = db;
            _siteUser = siteUser;
        }

        public List<SelectListItem> GetAssessmentTemplate(string templateType)
        {
            var assessmentTemplates = (
                            from assessmentTemplate in _db.tblAssessmentTemplates
                            where assessmentTemplate.TemplateType == templateType
                            orderby assessmentTemplate.TemplateType ascending
                            select new { AssessmentTemplateId = assessmentTemplate.AssessmentTemplateId, TemplateType = assessmentTemplate.TemplateType }
                            );
            var list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Text = "--SELECT--", Value = "" });
            foreach (var s in assessmentTemplates)
            {
                list.Add(new SelectListItem()
                {
                    Text = s.TemplateType,
                    Value = Convert.ToString(s.AssessmentTemplateId)
                });
            }

            return list;
        }
        /// <summary>
        /// This function is used to Add an Assessment
        /// </summary>
        /// <param name="studentExtend"></param>
        public string CreateAssessment(AssessmentViewModel model)
        {
            try
            {
                string resultMessage = string.Empty;
                int schoolTermId = int.Parse(model.SchoolTermId);
                int schoolYearId = int.Parse(model.SchoolYearId);
                int assessmentTypeId = int.Parse(model.AssessmentTypeId);
                int subjectId = int.Parse(model.SubjectId);

                if (!IsAssessmentAlreadyExists(schoolTermId, subjectId, assessmentTypeId, schoolYearId))
                {
                    string schoolTermDesc = _db.tblSchoolTerms.Where(x => x.SchoolTermId == schoolTermId).Select(x => x.SchoolTermDesc).FirstOrDefault();
                    string schoolYearDesc = _db.tblSchoolYears.Where(x => x.SchoolYearId == schoolYearId).Select(x => x.SchoolYearDesc).FirstOrDefault();
                    //only need to take two chracters of school year. ex: for 2014/15 we have to take 14/15
                    string schoolYear = schoolYearDesc.Substring(2, 5);

                    //Get all children, grandchildren etc. of parentAssessment
                    var assessmentTypes = GetHeirachicalAssessmentType(assessmentTypeId);

                    //Create and save assessment data for parent and each children.

                    foreach (var assessmentType in assessmentTypes)
                    {
                        string assessmentDesc = schoolTermDesc + " " + schoolYear + " " + assessmentType.AssessmentCode;
                        string assessmentSubDesc = assessmentDesc;
                        assessmentTypeId = assessmentType.AssessmentTypeId;

                        tblAssessment tblAssessment = new tblAssessment()
                        {
                            AssessmentDesc = assessmentDesc,
                            AssessmentSubDesc = assessmentSubDesc,
                            AssessmentTypeId = assessmentTypeId,
                            SubjectId = subjectId,
                            SchoolTermId = schoolTermId,
                            SchoolYearId = schoolYearId,
                            SLOTemplateId = model.SLOTemplateId == null ? (int?)null : Convert.ToInt32((model.SLOTemplateId)),
                            RubricTemplateId = model.RubricTemplateId == null ? (int?)null : Convert.ToInt32((model.RubricTemplateId)),
                            ScoreMin = model.ScoreMin,
                            ScoreMax = model.ScoreMax,
                            CreateDatetime = DateTime.UtcNow
                        };
                        //save assessment  
                        SaveAssessment(tblAssessment);
                    }
                    resultMessage = "Data Saved Successfully";
                }
                else
                {
                    resultMessage = "Assessment already exists for selected Subject, Assessment Type, School Year and School Term.";
                }
                return resultMessage;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private List<AssessmentType> GetHeirachicalAssessmentType(int assessmentTypeId)
        {
            List<AssessmentType> listAssessmentType = new List<AssessmentType>();
            AssessmentType assessmentType = null;
            var result = _db.GetHeirachicalAssessmentType(assessmentTypeId).ToList();
            result.ForEach(x =>
                {
                    assessmentType = new AssessmentType()
                    {
                        AssessmentCode = x.AssessmentCode,
                        AssessmentTypeId = x.AssessmentTypeId
                    };
                    listAssessmentType.Add(assessmentType);
                });
            return listAssessmentType;
        }

        private void SaveAssessment(tblAssessment tblAssessment)
        {
            using (var context = new dbTIREntities())
            {
                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        context.tblAssessments.Add(tblAssessment);
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

        private bool IsAssessmentAlreadyExists(int schoolTermId, int subjectId, int assessmentTypeId, int schoolYearId)
        {
            bool assessmentAlreadyExist = false;
            var result = _db.tblAssessments.Where(x => x.AssessmentTypeId == assessmentTypeId && x.SchoolTermId == schoolTermId &&
                                                  x.SubjectId == subjectId && x.SchoolYearId == schoolYearId)
                                            .Select(x => x.AssessmentId).ToList();
            if (result.Count > 0)
            {
                assessmentAlreadyExist = true;
            }
            return assessmentAlreadyExist;
        }
    }
}