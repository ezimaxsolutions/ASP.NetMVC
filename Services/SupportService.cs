using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EDS.Models;
using System.Data.Entity;
namespace EDS.Services
{
    public class SupportService
    {
        private SiteUser _siteUser;
        private dbTIREntities _db;

        public SupportService(SiteUser siteUser, dbTIREntities db)
        {
            _siteUser = siteUser;
            _db = db;
        }

        public IQueryable<Announcement> GetAnnouncements()
        {
            int userAssignedDistrict = _siteUser.Districts[0].Id;
            int userAssignedRole = (int)_siteUser.Role;
            int teacherRoleId = 0;

            if (!(_siteUser.isTeacher))
                teacherRoleId = (from x in _db.tblRoles where x.RoleDesc == HelperService.SiteRole_Teacher select x.RoleId).FirstOrDefault();


            var isSiteAdministrator = _siteUser.isEdsAdministrator;
            var isTeacher = _siteUser.isTeacher;
            var isAdmin = _siteUser.isAdministrator;
            var isDataAdmin = _siteUser.isDataAdministrator;

            var query = from a in _db.tblAnnouncements
                        where (a.DistrictId == null || a.DistrictId == userAssignedDistrict)
                          && (( isSiteAdministrator)
                            || (isDataAdmin && (a.RoleId >= userAssignedRole && a.RoleId <= teacherRoleId))
                            || (isAdmin && (a.RoleId >= userAssignedRole && a.RoleId <= teacherRoleId))
                            || (isTeacher && a.RoleId == userAssignedRole)
                            )
                        select new Announcement()
                        {
                            AnnouncementId = a.AnnouncementId,
                            Text = a.Text,
                            Title = a.Title,
                            CreatedDateTime = a.CreateDatetime
                        };

            return query.OrderByDescending(x => x.CreatedDateTime);
        }
    }
}