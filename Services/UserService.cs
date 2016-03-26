using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using EDS.Models;
using Microsoft.AspNet.Identity;
using System.Web.Mvc;
using System.Threading.Tasks;
using EntityFramework.Extensions;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.Entity;
using EDS.Helpers;

namespace EDS.Services
{
    public class UserService
    {
        private dbTIREntities _db;
        private SiteUser _siteUser;

        public UserService(SiteUser siteUser, dbTIREntities db)
        {
            _db = db;
            _siteUser = siteUser;
        }


        public List<tblUserExt> GetViewData(string schoolYearFilter, string schoolFilter, string searchFilter)
        {
            int userAssignedDistrict = _siteUser.Districts[0].Id;
            int schoolYear = Convert.ToInt32(schoolYearFilter);
            IQueryable<tblUserExt> query = (
                                            from user in _db.tblUsers
                                            join userDistrict in _db.tblUserDistricts on user.UserId equals userDistrict.UserId

                                            // Left join Role
                                            join role in _db.tblRoles on userDistrict.RoleId equals role.RoleId into rr
                                            from role in rr.DefaultIfEmpty()

                                            // Left join UserSchool
                                            join userSchool in _db.tblUserSchools on user.UserId equals userSchool.UserId into uu
                                            from userSchool in uu.DefaultIfEmpty()

                                            //join schoolYear to filter result on basis of schoolYear
                                            join tblschoolYear in _db.tblSchoolYears on userSchool.SchoolYearId equals tblschoolYear.SchoolYearId
                                            // Filter by user's district
                                            where userDistrict.DistrictId == userAssignedDistrict
                                            && tblschoolYear.SchoolYear == schoolYear

                                            select new EDS.tblUserExt()
                                            {
                                                AspNetUserId = user.AspNetUserId,
                                                FirstName = user.FirstName,
                                                LastName = user.LastName,
                                                RoleDesc = role.RoleDesc == null ? "--" : role.RoleDesc,
                                                UserEmail = user.UserEmail,
                                                UserId = user.UserId,
                                                StateId = user.StateId,
                                                SchoolYearId = tblschoolYear.SchoolYearId,
                                                // District and School are needed so we can filter results by District
                                                // and School but will result in duplicate rows. Dups are handled after
                                                // all filtering is complete.
                                                DistrictId = userDistrict.DistrictId,
                                                SchoolId = userSchool.SchoolId
                                            });


            // Filter by School
            int intTryParseResult;
            if (int.TryParse(schoolFilter, out intTryParseResult) && intTryParseResult != -1)
            {
                int[] onlyShowSchool = { intTryParseResult };
                query = query.Where(x => onlyShowSchool.Contains(x.SchoolId.Value));
            }
            else
            {
                if (_siteUser.isAdministrator || _siteUser.isDataAdministrator)
                {
                    int schoolYearId = _db.tblSchoolYears.Where(x => x.SchoolYear == schoolYear).Select(x => x.SchoolYearId).FirstOrDefault();
                    int[] schoolsForUser = _siteUser.Schools.Where(x => x.SchoolYearId == schoolYearId).Select(x => x.Id).ToArray();
                    query = query.Where(x => schoolsForUser.Contains(x.SchoolId.Value));
                    if (schoolsForUser.Count() == 0)
                    {
                        query = query.Where(x => x.SchoolId == intTryParseResult);
                    }
                }
            }


            if (!String.IsNullOrEmpty(searchFilter))
            {
                query = query.Where(x => x.FirstName.ToUpper().Contains(searchFilter.ToUpper()) || x.LastName.ToUpper().Contains(searchFilter.ToUpper()));
            }

            // model will have duplicate rows because a user can belong to
            // several districts. Remove dups by using userId to compare rows.
            IEqualityComparer<tblUserExt> comparerUserId = new EdsUserIdUserComparer();

            return query.AsEnumerable()
                        .Distinct(comparerUserId)
                        .ToList();
        }

        public void CreateEdsUser(string aspnetUserId, tblUserExt newUser)
        {
            try
            {
                // 1. Add user to tblUser
                tblUser newTblUser = new tblUser()
                {
                    CreateDatetime = DateTime.Now,
                    AspNetUserId = aspnetUserId,
                    FirstName = newUser.FirstName,
                    LastName = newUser.LastName,
                    UserEmail = newUser.UserEmail,
                    StateId = newUser.StateId,
                    LocalId = newUser.LocalId,
                    RoleId = newUser.RoleId
                };
                _db.tblUsers.Add(newTblUser);
                _db.SaveChanges();

                // Get new user
                int edsUserId = _db.tblUsers
                    .Where(x => x.AspNetUserId == aspnetUserId)
                    .Select(x => x.UserId)
                    .SingleOrDefault();
                int districtId = _siteUser.Districts[0].Id;

                newUser.UserId = edsUserId;


                using (var context = new dbTIREntities())
                {
                    using (var dbContextTransaction = context.Database.BeginTransaction())
                    {
                        try
                        {
                            // 2. Add user to tblUserDistrict
                            tblUserDistrict newTblUserDistrict = new tblUserDistrict()
                            {
                                UserId = edsUserId,
                                DistrictId = districtId,
                                CreatedDatetime = DateTime.Now,
                                ChangedDatetime = DateTime.Now,
                                SchoolYearId = newUser.SchoolYearId,
                                RoleId = (int)newUser.RoleId
                            };
                            context.tblUserDistricts.Add(newTblUserDistrict);

                            // 3. Add schools for this user
                            UpdateUserSchool(context, newUser);

                            // 4. Commit changes
                            dbContextTransaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            // 4. Rollback changes
                            dbContextTransaction.Rollback();
                            throw ex;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DeleteAspNetUser(aspnetUserId);
                throw new Exception(String.Format("ERROR: {0}", ex));
            }
        }

        public void DeleteAspNetUser(string aspnetUserId)
        {
            using (var context = new dbTIREntities())
            {
                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        context.tblUsers.Delete(x => x.AspNetUserId == aspnetUserId);
                        context.AspNetUserClaims.Delete(x => x.User_Id == aspnetUserId);
                        context.AspNetUserLogins.Delete(x => x.UserId == aspnetUserId);
                        context.AspNetUsers.Delete(x => x.Id == aspnetUserId);
                        dbContextTransaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // 4. Rollback changes
                        dbContextTransaction.Rollback();
                        throw new Exception(String.Format("Error in Rollback aspnet user. Error: {0}", ex.ToString()));
                    }
                }

            }

        }

        private void UpdateUserSchool(dbTIREntities context, tblUserExt newUser)
        {
            try
            {
                if (!String.IsNullOrEmpty(newUser.SelectedSchools))
                {
                    // 2. Delete all schools for this user
                    context.tblUserSchools.Delete(x => x.UserId == newUser.UserId && x.SchoolYearId == newUser.SchoolYearId);

                    // 3. Add schools
                    string[] schoolIds = newUser.SelectedSchools.Split(',');
                    foreach (var schoolId in schoolIds)
                    {
                        context.tblUserSchools.Add(new tblUserSchool()
                        {
                            SchoolId = Convert.ToInt32(schoolId),
                            UserId = newUser.UserId,
                            CreatedDatetime = DateTime.Now,
                            ChangedDatetime = DateTime.Now,
                            SchoolYearId = newUser.SchoolYearId
                        });
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error in UpdateUserSchool(). Error: {0}", ex.ToString()));
            }
        }

        // This function is used while Create User(post).
        public List<CheckBoxes> GetSelectedSchoolCheckBoxes(tblUserExt tblUserExtended)
        {
            var selectedSchools = tblUserExtended.SelectedSchools.Split(',').Select(int.Parse).ToArray();
            var schools = GetUserSchoolWithCheckBoxes(tblUserExtended, selectedSchools);
            return schools;
        }

        //This function is used while Edit User.
        public List<CheckBoxes> GetUserSchoolWithCheckBoxes(tblUserExt tblUserExtended)
        {
            var userSchools = _db.tblUserSchools.Where(x => x.UserId == tblUserExtended.UserId && x.SchoolYearId == tblUserExtended.SchoolYearId)
                                               .Select(x => x.SchoolId).ToArray();
            var schools = GetUserSchoolWithCheckBoxes(tblUserExtended, userSchools);
            return schools;
        }

        // Get data while create(post) and edit(get/post) 
        public List<CheckBoxes> GetUserSchoolWithCheckBoxes(tblUserExt tblUserExtended, int[] userSchools)
        {
            var query = GetSchoolsInfo(tblUserExtended);
            var schools = from q in query
                          select new CheckBoxes()
                          {
                              Id = q.Id,
                              Checked = userSchools.Contains(q.Id),
                              Text = q.Name,
                              UserId = q.UserId,
                              SchoolYearId = q.SchoolYearId,
                              IsLocked = q.UserId == null || tblUserExtended.SchoolYearId != q.SchoolYearId
                          };
            var result = schools.OrderBy(x => x.Text).ThenBy(x => x.IsLocked).ToList();
            result = RemoveDuplicateSchool(result);
            return result;
        }


        // Get data while create (get)
        public List<CheckBoxes> GetSchoolWithCheckBoxes(tblUserExt tblUserExtended)
        {
            var query = GetSchoolsInfo(tblUserExtended);
            var schools = from q in query
                          select new CheckBoxes()
                          {
                              Id = q.Id,
                              Text = q.Name,
                              UserId = (int?)q.UserId,
                              SchoolYearId = q.SchoolYearId,
                              IsLocked = q.UserId == null || tblUserExtended.SchoolYearId != q.SchoolYearId
                          };
            var result = schools.OrderBy(x => x.Text).ThenBy(x => x.IsLocked).ToList();
            result = RemoveDuplicateSchool(result);
            return result;
        }


        public IQueryable<UserSchool> GetSchoolsInfo(tblUserExt tblUserExtended)
        {
            int userAssignedDistrict = _siteUser.Districts[0].Id;
            var query = (from school in _db.tblSchools
                         join user in _db.tblUserSchools on school.SchoolId equals user.SchoolId into t
                         from ust in t.Where(ut => ut.UserId == _siteUser.EdsUserId).DefaultIfEmpty()
                         where school.DistrictId == userAssignedDistrict
                         select new UserSchool() { Id = school.SchoolId, Name = school.SchoolDesc, SchoolYearId = ust.SchoolYearId, UserId = ust.UserId });
            return query;
        }

        //remove duplicate schools from the list to display which are locked==========
        public List<CheckBoxes> RemoveDuplicateSchool(List<CheckBoxes> result)
        {
            int? prevSchoolId = -1;
            var schoolsToRemove = new List<CheckBoxes>();
            result.ForEach(s =>
            {
                if (prevSchoolId == s.Id)
                {
                    if (s.IsLocked)
                        schoolsToRemove.Add(s);
                }
                prevSchoolId = s.Id;
            });
            schoolsToRemove.ForEach(s => result.Remove(s));
            return result;
        }

        public void UpdateUser(tblUserExt tbluserExtended)
        {
            using (var context = new dbTIREntities())
            {
                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        var updateUser = context.tblUsers.Find(tbluserExtended.UserId);
                        if (updateUser != null)
                        {
                            // 1. Update user
                            updateUser.ChangeDatetime = DateTime.Now;
                            updateUser.FirstName = tbluserExtended.FirstName;
                            updateUser.LastName = tbluserExtended.LastName;
                            updateUser.UserEmail = tbluserExtended.UserEmail;
                            updateUser.StateId = tbluserExtended.StateId;
                            updateUser.LocalId = tbluserExtended.LocalId;
                            updateUser.RoleId = tbluserExtended.RoleId;
                            context.SaveChanges();

                            // 2. Update User district info
                            var updatedUserDistrict = context.tblUserDistricts.Where(x => x.UserId == tbluserExtended.UserId && x.SchoolYearId == tbluserExtended.SchoolYearId).FirstOrDefault();

                            if (updatedUserDistrict != null)
                            {
                                updatedUserDistrict.RoleId = (int)tbluserExtended.RoleId;
                                context.tblUserDistricts.Add(updatedUserDistrict);
                                context.Entry(updatedUserDistrict).State = EntityState.Modified;
                            }
                            else
                            {
                                // 3. Add district info 
                                int userAssignedDistrict = _siteUser.Districts[0].Id;
                                context.tblUserDistricts.Add(new tblUserDistrict()
                                {

                                    DistrictId = userAssignedDistrict,
                                    UserId = tbluserExtended.UserId,
                                    CreatedDatetime = DateTime.Now,
                                    ChangedDatetime = null,
                                    SchoolYearId = tbluserExtended.SchoolYearId,
                                    RoleId = (int)tbluserExtended.RoleId
                                });

                            }
                            context.SaveChanges();

                            // 4. Update User schools info
                            UpdateUserSchool(context, tbluserExtended);
                        }

                        // 3. Commit changes
                        dbContextTransaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // 4. Rollback changes
                        dbContextTransaction.Rollback();
                    }
                }
            }
        }

        public int GetRoleId(int? userId, int schoolYearId)
        {
            int userAssignedDistrict = _siteUser.Districts[0].Id;
            int roleId = _db.tblUserDistricts.Where(u => u.UserId == userId && u.SchoolYearId == schoolYearId && u.DistrictId == userAssignedDistrict)
                                             .Select(u => u.RoleId).FirstOrDefault();
            return roleId;
        }


        public bool IsUserHasPermissionForSchool(tblUserExt tblUserExtended)
        {
            bool isUserHasPermissionForSchool = true;
            int userAssignedDistrict = _siteUser.Districts[0].Id;
            var edsUserSchools = (from school in _db.tblSchools
                                  join userSchool in _db.tblUserSchools on school.SchoolId equals userSchool.SchoolId into t
                                  from ust in t.Where(ut => ut.UserId == _siteUser.EdsUserId).DefaultIfEmpty()
                                  where school.DistrictId == userAssignedDistrict && ust.SchoolYearId == tblUserExtended.SchoolYearId
                                  select school.SchoolId).ToList();

            var userSchools = _db.tblUserSchools.Where(x => x.UserId == tblUserExtended.UserId && x.SchoolYearId == tblUserExtended.SchoolYearId).Select(x=>x.SchoolId).ToList();
            var commonElements = edsUserSchools.Intersect(userSchools).ToList();
            if (commonElements.Count == 0)
            {
                isUserHasPermissionForSchool = false;
            }
            return isUserHasPermissionForSchool;
        }
    }
}