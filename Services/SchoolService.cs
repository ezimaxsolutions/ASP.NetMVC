using EDS.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using System.Configuration;

namespace EDS.Services
{
    public class SchoolService
    {
        private SiteUser _siteUser;
        private dbTIREntities _db;

        public SchoolService(SiteUser siteUser, dbTIREntities db)
        {
            _siteUser = siteUser;
            _db = db;
        }

        /// <summary>
        /// Gets the current school year from tblSchoolYear.
        /// </summary>
        /// <returns>tblSchoolYear.SchoolYear as a String</returns>
        public string GetCurrentSchoolYear()
        {
            int currentSchoolYear = Convert.ToInt32(ConfigurationManager.AppSettings["SchoolYear"].ToString());
            return _db.tblSchoolYears.Where(x => x.SchoolYear == currentSchoolYear).Select(x => x.SchoolYear).FirstOrDefault().ToString();
        }

        /// <summary>
        /// Pulls all rows from tblSchoolYear and wraps results
        /// as MultiSelectList. Return data is meant for an
        /// Html.DropDownList.
        /// </summary>
        /// <param name="selectedItem">Value to preselect in drop down.</param>
        /// <returns>MultiSelectList</returns>
        public IEnumerable DropDownDataSchoolYear(string selectedItem)
        {
            var query = _db.tblSchoolYears.OrderByDescending(x => x.SchoolYear);
            return new MultiSelectList(query, "SchoolYear", "SchoolYearDesc", new string[] { selectedItem });
        }


        /// <summary>
        /// Pulls all rows from tblSchoolYear and wraps results
        /// as MultiSelectList. Return data is meant for an
        /// Html.DropDownList.
        /// </summary>
        /// <param name="selectedItem">Value to preselect in drop down.</param>
        /// <returns>MultiSelectList</returns>
        public IEnumerable DropDownDataSchoolYearID(int selectedItem)
        {
            var query = _db.tblSchoolYears.OrderByDescending(x => x.SchoolYear);
            int selectedYearId = query.Where(x => x.SchoolYear == selectedItem).FirstOrDefault().SchoolYearId;
            return new MultiSelectList(query, "SchoolYearId", "SchoolYearDesc", new string[] { Convert.ToString(selectedItem) });
        }


        public IEnumerable GetSchoolYearDownData(int schoolYearID)
        {
            var query = _db.tblSchoolYears.OrderByDescending(x => x.SchoolYear);
            return new MultiSelectList(query, "SchoolYearId", "SchoolYearDesc", new string[] { Convert.ToString(schoolYearID) });
        }

        /// <summary>
        /// Pulls all rows from tblSchools and wraps results
        /// as MultiSelectList. Return data is meant for an
        /// Html.DropDownList. Results are based on user's role.
        /// </summary>
        /// <param name="selectedItem">Value to preselect in drop down.</param>
        /// <returns>MultiSelectList</returns>
        public IEnumerable DropDownDataSchool(string selectedItem)
        {
            IQueryable schoolQuery;
            var districtsForUser = _siteUser.Districts.Select(x => x.Id).ToList();

            if (_siteUser.isTeacher || _siteUser.isAdministrator || _siteUser.isDataAdministrator)
            {
                List<int> schoolsForUser = _siteUser.Schools.Select(x => x.Id).ToList();
                schoolQuery = _db.tblSchools
                    .Where(x => districtsForUser.Contains(x.DistrictId))
                    .Where(x => schoolsForUser.Contains(x.SchoolId))
                    .OrderBy(x => x.SchoolDesc);
            }
            else
            {
                schoolQuery = _db.tblSchools
                    .Where(x => districtsForUser.Contains(x.DistrictId))
                    .OrderBy(x => x.SchoolDesc);
            }

            IEnumerable<SelectListItem> dropDownDefault = Enumerable.Repeat(
                new SelectListItem { Value = "-1", Text = "--All--" }, count: 1);

            return dropDownDefault.Concat(new MultiSelectList(
                    schoolQuery,
                    "SchoolId", "SchoolDesc", new string[] { selectedItem }));
        }

        /// <summary>
        /// Pulls all rows from tblSchools and wraps results
        /// as MultiSelectList. 
        /// </summary>
        /// <param name="selectedItem">Value to preselect in drop down.</param>
        /// <returns>MultiSelectList</returns>
        public IEnumerable DropDownDataSchoolForClass(string selectedItem)
        {
            IQueryable schoolQuery;
            var districtsForUser = _siteUser.Districts.Select(x => x.Id).ToList();

            if (_siteUser.isTeacher || _siteUser.isAdministrator || _siteUser.isDataAdministrator)
            {
                List<int> schoolsForUser = _siteUser.Schools.Select(x => x.Id).ToList();
                schoolQuery = _db.tblSchools
                    .Where(x => districtsForUser.Contains(x.DistrictId))
                    .Where(x => schoolsForUser.Contains(x.SchoolId))
                    .OrderBy(x => x.SchoolDesc);
            }
            else
            {
                schoolQuery = _db.tblSchools
                    .Where(x => districtsForUser.Contains(x.DistrictId))
                    .OrderBy(x => x.SchoolDesc);
            }

            return new MultiSelectList(schoolQuery, "SchoolId", "SchoolDesc", new string[] { selectedItem });
        }
    }
}