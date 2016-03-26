using System.Data.Entity.SqlServer;
using System.Net.Mime;
using EDS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EDS.Services
{
    public class CommonService
    {
        private SiteUser _siteUser;
        private dbTIREntities _db;

        public CommonService(SiteUser siteUser, dbTIREntities db)
        {
            _db = db;
            _siteUser = siteUser;
        }
        public List<SelectListItem> GetSubjects()
        {
            var subjects = (from subject in _db.tblSubjects
                            select new { SubjectId = subject.SubjectId, SubjectDesc = subject.SubjectDesc }
                            );
            var list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Text = "--SELECT--", Value = "" });
            foreach (var s in subjects)
            {
                list.Add(new SelectListItem()
                {
                    Text = s.SubjectDesc,
                    Value = Convert.ToString(s.SubjectId)
                });
            }
            return list;
        }
        public List<SelectListItem> GetAssessmentType()
        {
            var assessmentTypes = (
                            from assessmentType in _db.tblAssessmentTypes
                            where assessmentType.ParentAssessmentTypeId == null
                            orderby assessmentType.AssessmentTypeDesc ascending
                            select new { AssessmentTypeId = assessmentType.AssessmentTypeId, AssessmentTypeDesc = assessmentType.AssessmentTypeDesc }
                            );

            var list = new List<SelectListItem>();
            list.Add(new SelectListItem() { Text = "--SELECT--", Value = "" });
            foreach (var a in assessmentTypes)
            {
                list.Add(new SelectListItem()
                {
                    Text = a.AssessmentTypeDesc,
                    Value = Convert.ToString(a.AssessmentTypeId)
                });
            }
            return list;
        }
        public List<SelectListItem> GetSchoolTerm()
        {
            var schoolTerms = (
                            from schoolTerm in _db.tblSchoolTerms
                            orderby schoolTerm.OrderBy
                            select schoolTerm)
                             .Select(x =>
                            new SelectListItem
                            {
                                Text = x.SchoolTermDesc,
                                Value = SqlFunctions.StringConvert((decimal)x.SchoolTermId)
                            })
                            .ToList();
            schoolTerms.Insert(0, new SelectListItem { Value = "", Text = "--SELECT--" });
            return schoolTerms;
        }
        public List<SelectListItem> GetSchoolYear()
        {
            var schoolYears = (
                            from schoolYear in _db.tblSchoolYears
                            orderby schoolYear.SchoolYear descending
                            select schoolYear
                            )
                             .Select(x =>
                            new SelectListItem
                            {
                                Text = x.SchoolYearDesc,
                                Value = SqlFunctions.StringConvert((decimal)x.SchoolYearId)
                            })
                            .ToList();
            schoolYears.Insert(0, new SelectListItem { Value = "", Text = "--SELECT--" });
            return schoolYears;
        }

        public List<SelectListItem> GetAssessment()
        {
            var assessments = (
                            from assessment in _db.tblAssessments
                            select assessment
                            )
                             .Select(x =>
                            new SelectListItem
                            {
                                Text = x.AssessmentDesc,
                                Value = SqlFunctions.StringConvert((decimal)x.AssessmentId)
                            })
                            .ToList();

            assessments.Insert(0, new SelectListItem { Value = "", Text = "--SELECT--" });
            return assessments;
        }
        public List<SelectListItem> GetDistrict()
        {
            var districts = (
                            from d in _db.tblDistricts
                            select d
                            )
                            .Select(x =>
                            new SelectListItem
                            {
                                Text = x.DistrictDesc,
                                Value = SqlFunctions.StringConvert((decimal)x.DistrictId)
                            }
                            ).ToList();
            districts.Insert(0, new SelectListItem { Value = "", Text = "--SELECT--" });
            return districts;
        }

        public List<SelectListItem> GetSchools()
        {
            var schools = (from s in _db.tblSchools
                           select s
                )
                .Select(x =>
                new SelectListItem
                {
                    Value = SqlFunctions.StringConvert((decimal)x.SchoolId),
                    Text = x.SchoolDesc
                }
                ).ToList();
            schools.Insert(0, new SelectListItem { Value = "", Text = "--SELECT--" });
            return schools;
        }


        public List<SelectListItem> GetClasses()
        {
            var schools = (from s in _db.tblClasses
                           select s
                )
                .Select(x =>
                new SelectListItem
                {
                    Value = SqlFunctions.StringConvert((decimal)x.ClassId),
                    Text = x.ClassDesc
                }
                ).ToList();

            schools.Insert(0, new SelectListItem { Value = "", Text = "--SELECT--" });
            return schools;
        }

    }
}