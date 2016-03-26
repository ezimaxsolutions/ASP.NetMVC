using EDS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq.SqlClient;
using System.Web;

namespace EDS.Services
{
    public class KnowledgeBaseService
    {

        private SiteUser _siteUser;
        private dbTIREntities _db;

        public KnowledgeBaseService(SiteUser siteUser, dbTIREntities db)
        {
            _siteUser = siteUser;
            _db = db;
        }

        public IQueryable<KnowledgeBase> GetKnowledgeBase()
        {
            //TODO: in case multiple districts supported, we need refactor
            int userAssignedDistrict = _siteUser.Districts[0].Id;
            int userAssignedRole = (int)_siteUser.Role;

            int teacherRoleId = 0;

            if (!(_siteUser.isTeacher))
                teacherRoleId = (from x in _db.tblRoles where x.RoleDesc == HelperService.SiteRole_Teacher select x.RoleId).FirstOrDefault();

            var isSiteAdministrator = _siteUser.isEdsAdministrator;
            var isTeacher = _siteUser.isTeacher;
            var isAdmin = _siteUser.isAdministrator;
            var isDataAdmin = _siteUser.isDataAdministrator;

            var query = from kb in _db.tblKnowledgeBases
                        where (kb.DistrictId == null || kb.DistrictId == userAssignedDistrict)
                         && (( isSiteAdministrator)
                            || (isDataAdmin && (kb.RoleId >= userAssignedRole && kb.RoleId <= teacherRoleId))
                            || (isAdmin && (kb.RoleId >= userAssignedRole && kb.RoleId <= teacherRoleId))
                            || (isTeacher && kb.RoleId == userAssignedRole)
                            )
                        select new KnowledgeBase
                        {
                            KnowledgeBaseId = kb.KnowledgeBaseId,
                            Text = kb.Text,
                            Title = kb.Title,
                            CreatedDateTime = kb.CreateDatetime
                        };
           
            return query.OrderByDescending(x => x.CreatedDateTime);
        }

        public KnowledgeBase GetKnowledgeBaseDetail(int knowledgeBaseId, int districtId)
        {
            KnowledgeBase knowledgeBase = null;
            var query = from kb in _db.tblKnowledgeBases
                        where kb.KnowledgeBaseId == knowledgeBaseId
                            && (kb.DistrictId == districtId || kb.DistrictId == null)
                        select new KnowledgeBase
                        {
                            KnowledgeBaseId = kb.KnowledgeBaseId,
                            Text = kb.Text,
                            Title = kb.Title,
                            CreatedDateTime = kb.CreateDatetime,
                        };
            var dataKnowledgeBase = query.FirstOrDefault();
            knowledgeBase = dataKnowledgeBase;
            if (dataKnowledgeBase != null)
            {
                var attQuery = from kba in _db.tblKnowledgeBaseAttachments
                               join att in _db.tblAttachments on kba.AttachmentId equals att.AttachmentId
                               where kba.KnowledgeBaseId == dataKnowledgeBase.KnowledgeBaseId
                               select new FileDetail
                               {
                                   FileExtension = att.FileExtension,
                                   PhysicalFileName = att.FileName,
                                   OriginalFileName = att.OriginalFileName
                               };
                var attachments = attQuery.ToList();
                knowledgeBase.FileDetails = attachments;
            }
            return knowledgeBase;

        }

        /// <summary>
        /// SaveKnowledgeBase() method is used to save knowledgebase data
        /// </summary>
        /// <param name="KnowledgeBase"></param>
        public void SaveKnowledgeBase(KnowledgeBase knowledgeBase)
        {

            using (var dbContextTransaction = _db.Database.BeginTransaction())
            {
                try
                {
                    tblKnowledgeBase objKnowledgeBase = null;
                    objKnowledgeBase = new tblKnowledgeBase()
                                        {
                                            Title = knowledgeBase.Title,
                                            Text = knowledgeBase.Text,
                                            DistrictId = knowledgeBase.DistrictId,
                                            RoleId = knowledgeBase.RoleId,
                                            CreateDatetime = knowledgeBase.CreatedDateTime,
                                        };
                    _db.tblKnowledgeBases.Add(objKnowledgeBase);
                    _db.SaveChanges();
                    tblAttachment objAttachment = null;

                    if (knowledgeBase.FileDetails != null)
                    {
                        foreach (var file in knowledgeBase.FileDetails)
                        {
                            objAttachment = new tblAttachment()
                            {
                                CreatedDate = knowledgeBase.CreatedDateTime,
                                CreatedUserId = knowledgeBase.CreatedUserId,
                                FileName = file.PhysicalFileName,
                                OriginalFileName = file.OriginalFileName,
                                FileExtension = file.FileExtension
                            };

                            _db.tblAttachments.Add(objAttachment);
                            _db.SaveChanges();

                            tblKnowledgeBaseAttachment objKnowledgeBaseAttachment = null;
                            objKnowledgeBaseAttachment = new tblKnowledgeBaseAttachment()
                                                        {
                                                            AttachmentId = objAttachment.AttachmentId,
                                                            KnowledgeBaseId = objKnowledgeBase.KnowledgeBaseId,
                                                            CreatedDate = knowledgeBase.CreatedDateTime,
                                                            CreatedUserId = knowledgeBase.CreatedUserId
                                                        };
                            _db.tblKnowledgeBaseAttachments.Add(objKnowledgeBaseAttachment);
                            _db.SaveChanges();
                        }
                    }
                    dbContextTransaction.Commit();
                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();
                    throw ex;
                }
            }
        }

        /// <summary>
        /// IsKnowledgeBaseExists() method is used to check if knowledgebase with same title already exists. Title should be unique.
        /// </summary>
        /// <param name="kbTitle"></param>
        /// <returns></returns>
        public bool IsKnowledgeBaseExists(string kbTitle)
        {
            int userAssignedDistrict = _siteUser.Districts[0].Id;
            var count = (from x in _db.tblKnowledgeBases
                         where x.Title.Contains(kbTitle.Trim())
                         select x).Count();
            return (count > 0 ? true : false);
        }
    }
}