using EDS.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;

namespace EDS.Services
{
    public static class HelperService
    {
        public static string SiteRole_Administrator = ConfigurationManager.AppSettings["SiteRoleAdministrator"];
        public static string SiteRole_DataAdministrator = ConfigurationManager.AppSettings["SiteRoleDataAdministrator"];
        public static string SiteRole_EdsAdministrator = ConfigurationManager.AppSettings["SiteRoleEdsAdministrator"];
        public static string SiteRole_Teacher = ConfigurationManager.AppSettings["SiteRoleTeacher"];
        public static string SiteDecimal_Place = ConfigurationManager.AppSettings["SiteDecimalPlace"];


        /// <summary>
        /// Used for populating School drop downs. A value of -1 indicates user selected --ALL--.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userDistrictId"></param>
        /// <param name="schoolId"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static int[] GetUserSchoolsFilter(int userId, int userDistrictId, int schoolId, dbTIREntities db)
        {
            if (schoolId == -1)
            {
                // Get all schools for the district
                return (from school in db.tblSchools
                        where school.DistrictId == userDistrictId
                        select school.SchoolId).ToArray();
            }
            else
            {
                return new int[] { schoolId };
            }
        }

        /// <summary>
        /// Used in UI Javascript to disable and or hide buttons and input fields.
        /// </summary>
        /// <param name="userRole"></param>
        /// <param name="sitePage"></param>
        /// <returns></returns>
        public static bool AllowUiEdits(string userRole, string sitePage)
        {
            bool allow = false;
            switch (sitePage.ToUpper())
            {
                case "STUDENT":
                    allow = (userRole.Equals(SiteRole_DataAdministrator, StringComparison.OrdinalIgnoreCase) ||
                            userRole.Equals(SiteRole_EdsAdministrator, StringComparison.OrdinalIgnoreCase));
                    break;
                case "CLASS":
                case "USER":
                    allow = (userRole.Equals(SiteRole_DataAdministrator, StringComparison.OrdinalIgnoreCase) ||
                             userRole.Equals(SiteRole_EdsAdministrator, StringComparison.OrdinalIgnoreCase));
                    break;
                case "REPORT":
                    allow = (userRole.Equals(SiteRole_DataAdministrator, StringComparison.OrdinalIgnoreCase) ||
                             userRole.Equals(SiteRole_EdsAdministrator, StringComparison.OrdinalIgnoreCase) ||
                             userRole.Equals(SiteRole_Administrator, StringComparison.OrdinalIgnoreCase));
                    break;
            }

            return allow;
        }

        /// <summary>
        /// Pull current year from the database.
        /// TODO: Web.config must not be part of determining current year.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static string SchoolYearDescription(dbTIREntities db)
        {
            int currentSchoolYear = Convert.ToInt32(ConfigurationManager.AppSettings["SchoolYear"].ToString());
            var schoolYear = db.tblSchoolYears.Where(y => y.SchoolYear == currentSchoolYear)
                .Select(d => d.SchoolYearDesc).ToList();
            return schoolYear[0].ToString();
        }

        public static int SchoolYear()
        {
            // TODO: Get from database.
            return Convert.ToInt32(ConfigurationManager.AppSettings["SchoolYear"].ToString());
        }

        /// <summary>
        /// Clears user profile from Session.
        /// Used when user logs off the site.
        /// </summary>
        public static void RemoveSiteUserProfile()
        {
            HttpContext.Current.Session["SiteUser"] = null;
        }

        /// <summary>
        /// Save user profile in Session.
        /// Used when user logs in the site.
        /// </summary>
        /// <param name="aspnetUser"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static SiteUser SetSiteUserProfile(ApplicationUser aspnetUser, dbTIREntities db)
        {
            if (HttpContext.Current.Session["SiteUser"] == null)
            {
                var su = GetUserProfile(aspnetUser, db);
                HttpContext.Current.Session["SiteUser"] = su;
            }
            return (SiteUser)HttpContext.Current.Session["SiteUser"];
        }

        public static bool IsUserRoleValid(string userRole, string securityRole)
        {
            return (securityRole.IndexOf(userRole, 0) > -1);
        }

        /// <summary>
        /// Pulls user profile from ASPNet Identity and TIR databases.
        /// </summary>
        /// <param name="aspnetUser"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static SiteUser GetUserProfile(ApplicationUser aspnetUser, dbTIREntities db)
        {
            int currentSchoolYear = SchoolYear();
            int schoolYearId = GetSchoolYearId(currentSchoolYear, new dbTIREntities());

            var edsUserId = (from e in db.tblUsers
                             where e.AspNetUserId == aspnetUser.Id
                             select e.UserId).SingleOrDefault();


            var edsUser = (from user in db.tblUsers
                           join ud in db.tblUserDistricts on user.UserId equals ud.UserId
                           join role in db.tblRoles on ud.RoleId equals role.RoleId
                           where user.AspNetUserId == aspnetUser.Id && ud.SchoolYearId == schoolYearId
                           select new
                           {
                               FullName = user.FirstName + " " + user.LastName,
                               RoleId = role.RoleId,
                               RoleDesc = role.RoleDesc
                           }).FirstOrDefault();
            SiteUser siteUser = new SiteUser()
            {
                EdsUserId = edsUserId,
                IdentityUserId = aspnetUser.Id,
                IdentityUserName = aspnetUser.UserName,
                UserFullName = edsUser.FullName,
                Role = edsUser.RoleId,
                RoleDesc = edsUser.RoleDesc,
                isAdministrator = edsUser.RoleDesc.Equals(SiteRole_Administrator, StringComparison.OrdinalIgnoreCase),
                isDataAdministrator = edsUser.RoleDesc.Equals(SiteRole_DataAdministrator, StringComparison.OrdinalIgnoreCase),
                isEdsAdministrator = edsUser.RoleDesc.Equals(SiteRole_EdsAdministrator, StringComparison.OrdinalIgnoreCase),
                isTeacher = edsUser.RoleDesc.Equals(SiteRole_Teacher, StringComparison.OrdinalIgnoreCase),

                Districts = (from userDistrict in db.tblUserDistricts
                             join district in db.tblDistricts on userDistrict.DistrictId equals district.DistrictId
                             where userDistrict.UserId == edsUserId && userDistrict.SchoolYearId == schoolYearId
                             select new UserDisctrict()
                             {
                                 Id = district.DistrictId,
                                 Name = district.DistrictDesc
                             }).ToList(),

                Schools = (from userSchool in db.tblUserSchools
                           join school in db.tblSchools on userSchool.SchoolId equals school.SchoolId
                           where userSchool.UserId == edsUserId
                           select new UserSchool()
                           {
                               Id = userSchool.SchoolId,
                               Name = school.SchoolDesc,
                               DistrictId = school.DistrictId,
                               SchoolYearId = userSchool.SchoolYearId
                           }).ToList()
            };

            return siteUser;
        }

        public static void UpdateSiteUserProfile(SiteUser su, dbTIREntities db)
        {
            SiteUser siteUser = su;
            siteUser.Schools = (from userSchool in db.tblUserSchools
                                join school in db.tblSchools on userSchool.SchoolId equals school.SchoolId
                                where userSchool.UserId == su.EdsUserId
                                select new UserSchool()
                                {
                                    Id = userSchool.SchoolId,
                                    Name = school.SchoolDesc,
                                    DistrictId = school.DistrictId,
                                    SchoolYearId = userSchool.SchoolYearId
                                }).ToList();

            HttpContext.Current.Session["SiteUser"] = siteUser;

        }
        public static string GetDecimalDisplayFormat(RoundingType roundingType)
        {
            var decimalFormat = "0";
            if (roundingType == RoundingType.SiteDefaultDecimalPlace)
            {
                var decimalPlace = int.Parse(SiteDecimal_Place);
                if (decimalPlace > 0)
                {
                    decimalFormat += "." + new string('0', decimalPlace);
                }
            }
            else if (roundingType == RoundingType.SingleDecimalPlace)
                decimalFormat += ".0";
            return decimalFormat;
        }
        public static decimal GetRoundedValue(RoundingType roundingType, decimal value)
        {
            if (roundingType == RoundingType.SiteDefaultDecimalPlace)
                value = Math.Round(value, int.Parse(SiteDecimal_Place));
            else if (roundingType == RoundingType.DoubleDecimalplace)
                value = Math.Round(value, 2);
            else if (roundingType == RoundingType.SingleDecimalPlace)
                value = Math.Round(value, 1);
            else if (roundingType == RoundingType.NoDecimalPlace)
                value = Math.Round(value, 0);

            return value;
        }
        public static void SetUserLoginInfo(SiteUser siteUser, dbTIREntities db)
        {
            var userLoggedInInfo = db.tblUserLoginInfoes.Where(l => l.UserId == siteUser.EdsUserId).FirstOrDefault();
            if (userLoggedInInfo != null)
            {
                userLoggedInInfo.LoginDate = DateTime.Now.ToUniversalTime();
                db.Entry(userLoggedInInfo).State = EntityState.Modified;
            }
            else
            {
                tblUserLoginInfo userLoginInfo = new tblUserLoginInfo()
                                                 {
                                                     UserId = siteUser.EdsUserId,
                                                     LoginDate = DateTime.Now.ToUniversalTime()
                                                 };
                db.tblUserLoginInfoes.Add(userLoginInfo);
            }
            db.SaveChanges();
        }

        public static bool IsUserAuthorized(ApplicationUser aspnetUser, dbTIREntities db)
        {
            int currentSchoolYear = SchoolYear();
            int schoolYearId = GetSchoolYearId(currentSchoolYear, new dbTIREntities());
            bool isUserAuthorized = false;
            var userInfo = (from user in db.tblUsers
                            join ud in db.tblUserDistricts on user.UserId equals ud.UserId
                            where user.AspNetUserId == aspnetUser.Id && ud.SchoolYearId == schoolYearId
                            select ud
                          ).FirstOrDefault();
            if (userInfo != null)
            {
                isUserAuthorized = true;
            }
            return isUserAuthorized;
        }

        public static int GetSchoolYearId(int schoolYear, dbTIREntities db)
        {
            return db.tblSchoolYears.Where(x => x.SchoolYear == schoolYear)
                                                   .Select(x => x.SchoolYearId).FirstOrDefault();
        }


    }
    public enum RoundingType
    {
        SiteDefaultDecimalPlace,
        DoubleDecimalplace,
        SingleDecimalPlace,
        NoDecimalPlace,
        Default
    }
}