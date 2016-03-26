using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EDS.Models;
using System.Data.SqlClient;
using System.Configuration;
using System.Data.Entity;
using System.Collections;

namespace EDS.Services
{
    public class AssessmentClassScoreService
    {
        private SiteUser _siteUser;
        private dbTIREntities _db;

        public AssessmentClassScoreService(SiteUser siteUser, dbTIREntities db)
        {
            _db = db;
            _siteUser = siteUser;
        }
        public List<AssessmentClassScoreDetail> GetClassAssessmentScoreData(int districtId, AssessmentClassScoreViewModel model)
        {
            //R1 = this is the set of data for the selected term which physically exists
            var existingAssessmentClassScoreDetails = GetExistingClassAssessmentScoreData(districtId, model);

            //R2 = get all the list of assessmentTypes (parent+child) and students who have or not have score in all terms satisfying the input search params.
            //this means union of all assessmentTypes and students for all terms satisfying the input search params (excluding the termid param)
            //var allStudentsAssessmentTypesInAllTerms = GetAllStudentsAssessmentTypesInAllTerms(model);

            //R3 = get the list of assessments for the selected school term.
            // this will be used to assign assessmentIds to students whose assesment score doesn't exist for a term, but assessments exists.
            //var schoolTermAssessments = GetSchoolTermAssessments(model);

            //Get final AssessmentRecords = [R1 union (R2 - R1)]
           //existingAssessmentClassScoreDetails = GetFinalAssessmentRecordAfterAssessmentMerging(existingAssessmentClassScoreDetails, allStudentsAssessmentTypesInAllTerms, schoolTermAssessments);

            //only those students in S which doesn't exist in R1
            //var allStudentIds = allStudentsAssessmentTypesInAllTerms.Select(s => s.StudentId).ToList();
            //var studentIdsWithExistingScoreInCurrentTerm = existingAssessmentClassScoreDetails.Select(s => s.StudentId).ToList();
           // var studentIdsWithoutAssessmentScoreInCurrentTerm = allStudentIds.Except(studentIdsWithExistingScoreInCurrentTerm).ToList();


            //finally merge students
            //existingAssessmentClassScoreDetails = GetFinalAssessmentRecordAfterStudentMerging(existingAssessmentClassScoreDetails, allStudentsAssessmentTypesInAllTerms, studentIdsWithoutAssessmentScoreInCurrentTerm, schoolTermAssessments);

            return existingAssessmentClassScoreDetails;
        }

        public List<AssessmentClassScoreDetail> GetExistingClassAssessmentScoreData(int districtId, AssessmentClassScoreViewModel model)
        {
            List<AssessmentClassScoreDetail> results = new List<AssessmentClassScoreDetail>();
            SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
            connection.Open();
            SqlCommand command = new SqlCommand("spGetClassAssessmentScore", connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@AssessmentTypeId", int.Parse(model.AssessmentTypeId)));
            command.Parameters.Add(new SqlParameter("@SubjectId", int.Parse(model.SubjectID)));
            command.Parameters.Add(new SqlParameter("@SchoolYearId", int.Parse(model.SchoolYearId)));
            command.Parameters.Add(new SqlParameter("@ClassID", int.Parse(model.ClassId)));
            command.Parameters.Add(new SqlParameter("@SchoolTermId", int.Parse(model.SchoolTermId)));
            SqlDataReader reader = command.ExecuteReader();
            AssessmentClassScoreDetail tempScoreDetail = null;
            int prevStudent = -1;
            int assessmentIndex = 0; //its range is [0:4]

            while (reader.Read())
            {
                var currentStudentId = Convert.ToInt32(reader["StudentId"]);
                if (prevStudent != currentStudentId)
                {
                    prevStudent = currentStudentId;
                    if (tempScoreDetail != null)
                    {
                        //creating new row
                        tempScoreDetail.AssessmentCount = assessmentIndex;
                        results.Add(tempScoreDetail);
                        assessmentIndex = 0;
                    }

                    tempScoreDetail = new AssessmentClassScoreDetail();
                    tempScoreDetail.StudentId = currentStudentId;
                    tempScoreDetail.StudentName = reader["StudentName"].ToString();
                    tempScoreDetail.LastName = reader["LastName"].ToString();
                    tempScoreDetail.ReportTemplateId = int.Parse(reader["ReportTemplateId"].ToString());
                }

                decimal? score = null;
                decimal? projection = null;
                int assessmentId;
                int assessmentTypeId;
                int assessmentScoreId;
                string assessmentDesc = null;
                string assessmentCode = null;
                int? scoreMin;
                int? scoreMax;
                int schoolTermId;
                int? gradeLevel;

                SetClassAssessmentScoreData(out score, out projection, out assessmentId, out assessmentTypeId, out assessmentDesc, out assessmentCode, out assessmentScoreId, out scoreMin, out scoreMax, out schoolTermId, out gradeLevel, reader);
                try
                {
                    //for N assessment column, the record will contain N ScoreData objects in the ScoreDatacollection
                    var propertyIndex = assessmentIndex + 1;
                    var scoreData = new ClassScoreData();
                    tempScoreDetail.ScoreDataCollection.Add(scoreData);

                    scoreData.RoundingType = HelperService.GetDecimalDisplayFormat(RoundingType.NoDecimalPlace);
                    scoreData.Score = score;
                    scoreData.Projection = projection;
                    scoreData.AssessmentDesc = assessmentDesc;
                    scoreData.AssessmentId = assessmentId;
                    scoreData.AssessmentTypeId = assessmentTypeId;
                    scoreData.AssessmentCode = assessmentCode;
                    scoreData.AssessmentScoreId = assessmentScoreId;
                    scoreData.ScoreMin = scoreMin;
                    scoreData.ScoreMax = scoreMax;
                    scoreData.SchoolTermId = schoolTermId;
                    scoreData.StudentId = currentStudentId;
                    scoreData.GradeLevel = gradeLevel;
                }
                catch (Exception ex)
                {
                    throw new Exception("Check if field exists belongs to correct assessment", ex);
                }
                assessmentIndex++;
            }
            if (tempScoreDetail != null)
            {
                tempScoreDetail.AssessmentCount = assessmentIndex;
                results.Add(tempScoreDetail);
            }
            reader.Close();
            connection.Close();
            return results;
        }
        //This method is used to get all students from all terms whose assessment score exists or not
        private List<StudentAndAssessmentType> GetAllStudentsAssessmentTypesInAllTerms(AssessmentClassScoreViewModel model)
        {
            List<StudentAndAssessmentType> allStudentsAssessmentTypesInAllTerms = new List<StudentAndAssessmentType>();
            //common proc spGetClassAssessmentScore is also used here by passing schoolTermId as null.
            var studentsAssessmentTypes = _db.GetClassAssessmentScore(int.Parse(model.AssessmentTypeId), int.Parse(model.SubjectID), int.Parse(model.SchoolYearId), int.Parse(model.ClassId), null).ToList();
            foreach (var d in studentsAssessmentTypes)
            {
                allStudentsAssessmentTypesInAllTerms.Add(new StudentAndAssessmentType
                {
                    AssessmentCode = d.AssessmentCode,
                    AssessmentTypeId = d.AssessmentTypeId,
                    LastName = d.LastName,
                    StudentName = d.StudentName,
                    StudentId = d.StudentId,
                    ReportTemplateId = d.ReportTemplateId,
                    AssessmentId = d.AssessmentId,
                    ScoreMin = d.ScoreMin,
                    ScoreMax = d.ScoreMax,
                    GradeLevel = (int)d.GradeLevel
                });
            }
            return allStudentsAssessmentTypesInAllTerms;
        }
        private List<SchoolTermAssessment> GetSchoolTermAssessments(AssessmentClassScoreViewModel model)
        {
            List<SchoolTermAssessment> schoolTermAssessments = new List<SchoolTermAssessment>();
            var schoolTermAssessmentData = _db.GetSchoolTermAssessment(int.Parse(model.AssessmentTypeId), int.Parse(model.SubjectID), int.Parse(model.SchoolYearId), int.Parse(model.SchoolTermId)).ToList();
            foreach (var d in schoolTermAssessmentData)
            {
                schoolTermAssessments.Add(new SchoolTermAssessment
                {
                    AssessmentCode = d.AssessmentCode,
                    AssessmentTypeId = d.AssessmentTypeId,
                    AssessmentId = d.AssessmentId,
                    ScoreMin = d.ScoreMin,
                    ScoreMax = d.ScoreMax
                });
            }
            return schoolTermAssessments;
        }
        private List<AssessmentClassScoreDetail> GetFinalAssessmentRecordAfterAssessmentMerging(List<AssessmentClassScoreDetail> existingAssessmentClassScoreDetails, List<StudentAndAssessmentType> allStudentsAssessmentTypesInAllTerms, List<SchoolTermAssessment> schoolTermAssessments)
        {
            try
            {
                //Sort by score data count to get maximum score records on top.
                existingAssessmentClassScoreDetails = existingAssessmentClassScoreDetails.OrderByDescending(sd => sd.ScoreDataCollection.Count()).ToList();

                AssessmentClassScoreDetail previousRecord = null;
                existingAssessmentClassScoreDetails.ForEach(record =>
                {
                    allStudentsAssessmentTypesInAllTerms.ForEach(assessmentType =>
                    {
                        var isAssessmentTypeAlreadyExistInRecord = record.ScoreDataCollection.Exists(sd => sd.AssessmentTypeId == assessmentType.AssessmentTypeId);
                        if (!isAssessmentTypeAlreadyExistInRecord)
                        {
                            var newScoreData = new ClassScoreData();
                            newScoreData.AssessmentTypeId = assessmentType.AssessmentTypeId;
                            newScoreData.AssessmentCode = assessmentType.AssessmentCode;
                            newScoreData.ScoreMin = assessmentType.ScoreMin;
                            newScoreData.ScoreMax = assessmentType.ScoreMax;
                            newScoreData.GradeLevel = assessmentType.GradeLevel;
                            newScoreData.StudentId = record.StudentId;
                            newScoreData.AssessmentId = 0;
                            //For assessment type who don't have score data for all records get assessmentId, ScoreMin and ScoreMax from previous record.
                            if (previousRecord != null)
                            {
                                var previousRecordWithSameAssessmentType = previousRecord.ScoreDataCollection.Find(sd => sd.AssessmentTypeId == assessmentType.AssessmentTypeId);
                                if (previousRecordWithSameAssessmentType != null)
                                {
                                    newScoreData.AssessmentId = previousRecordWithSameAssessmentType.AssessmentId;
                                    newScoreData.ScoreMin = previousRecordWithSameAssessmentType.ScoreMin;
                                    newScoreData.ScoreMax = previousRecordWithSameAssessmentType.ScoreMax;
                                }
                            }
                            else if (schoolTermAssessments.Count > 0)
                            {
                                var assessmentExist = schoolTermAssessments.Find(sa => sa.AssessmentTypeId == assessmentType.AssessmentTypeId);
                                if (assessmentExist != null)
                                {
                                    newScoreData.AssessmentId = assessmentExist.AssessmentId;
                                    newScoreData.ScoreMin = assessmentExist.ScoreMin;
                                    newScoreData.ScoreMax = assessmentExist.ScoreMax;
                                }
                            }
                            record.ScoreDataCollection.Add(newScoreData);
                        }
                    });
                    previousRecord = record;
                });

                existingAssessmentClassScoreDetails.ForEach(record =>
               {
                   var sortedScoreDataList = record.ScoreDataCollection.OrderBy(x => x.AssessmentTypeId).ToList();
                   record.ScoreDataCollection = sortedScoreDataList;
               });
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return existingAssessmentClassScoreDetails;
        }
        private List<AssessmentClassScoreDetail> GetFinalAssessmentRecordAfterStudentMerging(List<AssessmentClassScoreDetail> existingAssessmentClassScoreDetails, List<StudentAndAssessmentType> allStudentsAssessmentTypesInAllTerms, List<int> studentIdsWithoutAssessmentScoreInCurrentTerm, List<SchoolTermAssessment> schoolTermAssessments)
        {
            studentIdsWithoutAssessmentScoreInCurrentTerm.ForEach(studentId =>
            {
                var student = allStudentsAssessmentTypesInAllTerms.First(s => s.StudentId == studentId);
                var newAssessmentScoreClassDetial = new AssessmentClassScoreDetail
                {
                    LastName = student.LastName,
                    StudentId = student.StudentId,
                    StudentName = student.StudentName,
                    ReportTemplateId = student.ReportTemplateId
                };

                //new student score record should contain all the scoreData columns with empty/null data except gradeLevel
                var maximumAssessmentCount = existingAssessmentClassScoreDetails.Count > 0
                                             ? existingAssessmentClassScoreDetails[0].ScoreDataCollection.Count
                                             : allStudentsAssessmentTypesInAllTerms.Select(x => x.AssessmentTypeId).Distinct().Count();
                for (int i = 0; i < maximumAssessmentCount; i++)
                {
                    //set a default value of the id, code and gradeLevel
                    var referenceScoreData = new ClassScoreData { AssessmentId = 0, AssessmentCode = allStudentsAssessmentTypesInAllTerms[i].AssessmentCode, GradeLevel = allStudentsAssessmentTypesInAllTerms[i].GradeLevel };
                    //incase records already exists, then use id and code from one of the existing record
                    if (existingAssessmentClassScoreDetails.Count > 0)
                    {
                        referenceScoreData.AssessmentId = existingAssessmentClassScoreDetails[0].ScoreDataCollection[i].AssessmentId;
                        referenceScoreData.AssessmentCode = existingAssessmentClassScoreDetails[0].ScoreDataCollection[i].AssessmentCode;
                        referenceScoreData.ScoreMin = existingAssessmentClassScoreDetails[0].ScoreDataCollection[i].ScoreMin;
                        referenceScoreData.ScoreMax = existingAssessmentClassScoreDetails[0].ScoreDataCollection[i].ScoreMax;
                        referenceScoreData.GradeLevel = existingAssessmentClassScoreDetails[0].ScoreDataCollection[i].GradeLevel;
                    }
                    // if no reord found in existing record, then use assessmentId, ScoreMin and ScoreMax from Assessments found for a searched school term.
                    else if (schoolTermAssessments.Count > 0)
                    {
                        var assessmentExist = schoolTermAssessments.Find(sa => sa.AssessmentTypeId == allStudentsAssessmentTypesInAllTerms[i].AssessmentTypeId);
                        if (assessmentExist != null)
                        {
                            referenceScoreData.AssessmentId = assessmentExist.AssessmentId;
                            referenceScoreData.ScoreMin = assessmentExist.ScoreMin;
                            referenceScoreData.ScoreMax = assessmentExist.ScoreMax;
                        }
                    }
                    var newScoreData = new ClassScoreData
                    {
                        Score = null,
                        Projection = null,
                        AssessmentId = referenceScoreData.AssessmentId,
                        AssessmentCode = referenceScoreData.AssessmentCode,
                        StudentId = student.StudentId,
                        ScoreMin = referenceScoreData.ScoreMin,
                        ScoreMax = referenceScoreData.ScoreMax,
                        GradeLevel = referenceScoreData.GradeLevel
                    };
                    newAssessmentScoreClassDetial.ScoreDataCollection.Add(newScoreData);
                }
                //finally append the new record to existing result
                existingAssessmentClassScoreDetails.Add(newAssessmentScoreClassDetial);
            });
            return existingAssessmentClassScoreDetails;
        }

        private void SetClassAssessmentScoreData(out decimal? score, out decimal? projection, out int assessmentId, out int assessmentTypeId, out string assessmentDesc, out string assessmentCode, out int assessmentScoreId, out int? scoreMin, out int? scoreMax, out int schoolTermId, out int? gradeLevel, SqlDataReader reader)
        {
            score = (reader["Score"] == DBNull.Value) ? (decimal?)null : HelperService.GetRoundedValue(RoundingType.Default, Convert.ToDecimal(reader["Score"]));
            projection = (reader["Projection"] == DBNull.Value) ? (decimal?)null : HelperService.GetRoundedValue(RoundingType.Default, Convert.ToDecimal(reader["Projection"]));
            assessmentId = (reader["AssessmentId"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["AssessmentId"]);
            assessmentTypeId = Convert.ToInt32(reader["AssessmentTypeId"]);
            assessmentDesc = reader["AssessmentDesc"] == DBNull.Value ? null : reader["AssessmentDesc"].ToString();
            assessmentCode = reader["AssessmentCode"] == DBNull.Value ? null : reader["AssessmentCode"].ToString();
            assessmentScoreId = (reader["AssessmentScoreId"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["AssessmentScoreId"]);
            scoreMin = (reader["ScoreMin"] == DBNull.Value) ? (int?)null : Convert.ToInt32(reader["ScoreMin"]);
            scoreMax = (reader["ScoreMax"] == DBNull.Value) ? (int?)null : Convert.ToInt32(reader["ScoreMax"]);
            schoolTermId = Convert.ToInt32(reader["SchoolTermId"]);
            gradeLevel = (reader["GradeLevel"] == DBNull.Value) ? (int?)null : Convert.ToInt32(reader["GradeLevel"]);
        }
        internal bool IsAssessmentWeightingExists(AssessmentClassScoreViewModel model)
        {
            int assessmenttypeId = int.Parse(model.AssessmentTypeId);
            int subjectId = int.Parse(model.SubjectID);
            int schoolYearId = int.Parse(model.SchoolYearId);

            bool assessmentWeightingExist = false;
            var data = _db.tblAssessmentWeightings.Where(x => x.AssessmentTypeId == assessmenttypeId && x.SubjectId == subjectId && x.SchoolYearId == schoolYearId).ToList();
            if (data.Count > 0)
            {
                assessmentWeightingExist = true;
            }
            return assessmentWeightingExist;
        }
        public string SaveClassAssessmentScore(List<AssessmentClassScore> model)
        {
            string result = string.Empty;
            using (var context = new dbTIREntities())
            {
                try
                {
                    foreach (var m in model)
                    {
                        AddClassAssessmentScoreScore(m, out result);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Error in Rollback assessment class score. Error: {0}", ex.ToString()));
                }
            }
            return result;
        }
        private void AddClassAssessmentScoreScore(AssessmentClassScore assessmentScoreObj, out string result)
        {
            using (var context = new dbTIREntities())
            {
                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        //Update existing score
                        if (assessmentScoreObj.AssessmentScoreId != 0)
                        {
                            UpdateAssessmentClassScore(assessmentScoreObj);
                        }
                        // Save new Score
                        else
                        {
                            if (assessmentScoreObj.AssessmentScoreId == 0 && assessmentScoreObj.AssessmentId != 0)
                            {
                                SaveAssessmentClassScore(assessmentScoreObj);
                            }
                        }

                        result = "Records Saved Scuccessfully.";
                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback();
                        throw ex;
                    }
                }
            }
        }
        private void UpdateAssessmentClassScore(AssessmentClassScore assessmentScoreObj)
        {
            using (var context = new dbTIREntities())
            {
                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        var data = context.tblAssessmentScores
                                          .Where(x => x.AssessmentScoreId == assessmentScoreObj.AssessmentScoreId)
                                          .FirstOrDefault();

                        if (assessmentScoreObj.Score != EDS.Constants.SystemParameter.ScoreTargetDefaultValue.DefaultValue)
                        {
                            data.Score = assessmentScoreObj.Score;
                        }
                        if (assessmentScoreObj.Target != EDS.Constants.SystemParameter.ScoreTargetDefaultValue.DefaultValue)
                        {
                            data.Projection = assessmentScoreObj.Target;
                        }
                        data.ChangeDatetime = DateTime.UtcNow;
                        context.tblAssessmentScores.Add(data);
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
        private void SaveAssessmentClassScore(AssessmentClassScore assessmentScoreObj)
        {

            using (var context = new dbTIREntities())
            {
                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        int defaultValue = EDS.Constants.SystemParameter.ScoreTargetDefaultValue.DefaultValue;
                        int? score = assessmentScoreObj.Score != defaultValue ? assessmentScoreObj.Score : null;
                        int? projection = assessmentScoreObj.Target != defaultValue ? assessmentScoreObj.Target : null;
                        tblAssessmentScore tblAssessmentScore = new tblAssessmentScore()
                        {
                            AssessmentId = assessmentScoreObj.AssessmentId,
                            StudentId = assessmentScoreObj.StudentId,
                            Score = score,
                            Projection = projection,
                            GradeLevel = assessmentScoreObj.GradeLevel,
                            SchoolID = assessmentScoreObj.SchoolId,
                            CreateDatetime = DateTime.UtcNow
                        };
                        context.tblAssessmentScores.Add(tblAssessmentScore);
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
        public class AssessmentClassScoreComparer : IEqualityComparer<AssessmentClassScoreDetail>
        {
            public bool Equals(AssessmentClassScoreDetail x, AssessmentClassScoreDetail y)
            {
                //Check whether the objects are the same object.  
                if (Object.ReferenceEquals(x, y)) return true;

                //Check whether the products' properties are equal.  
                return x != null && y != null && x.StudentId.Equals(y.StudentId) && x.StudentName.Equals(y.StudentName);
            }
            public int GetHashCode(AssessmentClassScoreDetail obj)
            {
                //Get hash code for the Name field if it is not null.  
                int hashProductName = obj.StudentId == 0 ? 0 : obj.StudentId.GetHashCode();

                //Get hash code for the Code field.  
                int hashProductCode = obj.StudentId.GetHashCode();

                //Calculate the hash code for the product.  
                return hashProductName ^ hashProductCode;
            }
        }
    }
}