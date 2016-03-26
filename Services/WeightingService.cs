using EDS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace EDS.Services
{
    public class WeightingService
    {
        private SiteUser _siteUser;
        private dbTIREntities _db;

        public WeightingService(SiteUser siteUser, dbTIREntities db)
        {
            _db = db;
            _siteUser = siteUser;
        }


        public bool SaveWeightingDetail(WeightingViewModel model)
        {
            bool weightingAlreadyExist = false;
            int districtId = int.Parse(model.DistrictId);
            int schoolYearID = int.Parse(model.SchoolYearId);
            int subjectId = int.Parse(model.SubjectId);
            int assessmentTypeId = int.Parse(model.AssessmentTypeId);
            int grade = int.Parse(model.Grade);

            int assessmentWeightingId = _db.tblAssessmentWeightings
                                           .Where(x => x.DistrictId == districtId
                                                        && x.SchoolYearId == schoolYearID
                                                        && x.SubjectId == subjectId
                                                        && x.AssessmentTypeId == assessmentTypeId
                                                  )
                                          .Select(x => x.AssessmentWeightingId).FirstOrDefault();
            if (assessmentWeightingId == 0)
            {
                //Case 1: when data does not exist in tblAssessmentWeighting it also means it will not be there in tblAssessmentGradeWeighting, 
                          //then insert data in both tables. 
                CreateWeighting(model);
            }
            else
            {
                int assessmentGrdeWeightingId = _db.tblAssessmentGradeWeightings
                                                   .Where(x => x.AssessmentWeightingId == assessmentWeightingId && x.Grade == grade)
                                                   .Select(x => x.AssessmentGradeWeightingId).FirstOrDefault();
                if (assessmentGrdeWeightingId == 0)
                {
                    //Case 2: if data exists in tblAssessmentWeighting, and not in tblAssessmentGradeWeighting, then insert data in tblAssessmentGradeWeighting.
                    AddWeighting(model, assessmentWeightingId);
                }
                else if (assessmentWeightingId != 0 && assessmentGrdeWeightingId != 0)
                {
                    //Case 3: check if same data already exists.
                    var data = _db.tblAssessmentGradeWeightings
                                  .Where(x => x.AssessmentGradeWeightingId == assessmentGrdeWeightingId && x.Weighting == model.Weighting)
                                  .ToList();
                    if (data.Count > 0)
                    {
                        weightingAlreadyExist = true;
                    }
                    else
                    {
                        //Case 4: if data exists in both tables tblAssessmentWeighting and tblAssessmentGradeWeighting then update tblAssessmentGradeWeighting.
                        UpdateWeighting(model, assessmentGrdeWeightingId);
                    }
                }
            }
            return weightingAlreadyExist;
        }


        /// <summary>
        /// This function is used to add a new weighting
        /// </summary>
        /// <param name="studentExtend"></param>
        public void CreateWeighting(WeightingViewModel model)
        {
            using (var context = new dbTIREntities())
            {
                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        tblAssessmentWeighting tblWeighting = new tblAssessmentWeighting()
                        {
                            AssessmentTypeId = Convert.ToInt32(model.AssessmentTypeId),
                            SubjectId = Convert.ToInt32(model.SubjectId),
                            DistrictId = Convert.ToInt32(model.DistrictId),
                            SchoolYearId = Convert.ToInt32(model.SchoolYearId),
                            CreateDatetime = DateTime.UtcNow
                        };
                        context.tblAssessmentWeightings.Add(tblWeighting);
                        context.SaveChanges();

                        int assessmentWeightingId = tblWeighting.AssessmentWeightingId;
                        tblAssessmentGradeWeighting tblGradeWeighting = new tblAssessmentGradeWeighting()
                        {
                            AssessmentWeightingId = assessmentWeightingId,
                            Grade = Convert.ToInt16(model.Grade),
                            Weighting = (decimal)model.Weighting
                        };
                        context.tblAssessmentGradeWeightings.Add(tblGradeWeighting);
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
        /// <summary>
        /// This function is used to add weighting data in tblAssessmentGradeWeighting
        /// </summary>
        /// <param name="model"></param>
        public void AddWeighting(WeightingViewModel model, int assessmentWeightingId)
        {
            using (var context = new dbTIREntities())
            {
                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        tblAssessmentGradeWeighting tblGradeWeighting = new tblAssessmentGradeWeighting()
                        {
                            AssessmentWeightingId = assessmentWeightingId,
                            Grade = Convert.ToInt16(model.Grade),
                            Weighting = (decimal)model.Weighting
                        };
                        context.tblAssessmentGradeWeightings.Add(tblGradeWeighting);
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

        /// <summary>
        /// This function is used to update weighting data in tblAssessmentGradeWeighting
        /// </summary>
        /// <param name="model"></param>
        public void UpdateWeighting(WeightingViewModel model, int assessmentGrdeWeightingId)
        {
            using (var context = new dbTIREntities())
            {
                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        var data = context.tblAssessmentGradeWeightings
                                          .Where(x => x.AssessmentGradeWeightingId == assessmentGrdeWeightingId)
                                          .FirstOrDefault();

                        data.Weighting = (decimal)model.Weighting;
                        context.tblAssessmentGradeWeightings.Add(data);
                        context.Entry(data).State = EntityState.Modified;
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