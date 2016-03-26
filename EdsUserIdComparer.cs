using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EDS.Models;

namespace EDS
{
    public class EdsUserIdUserComparer : IEqualityComparer<tblUserExt>
    {
        public bool Equals(tblUserExt x, tblUserExt y)
        {
            return x.UserId.Equals(y.UserId);
        }

        public int GetHashCode(tblUserExt obj)
        {
            return obj.UserId.GetHashCode();
        }
    }

    public class EdsUserIdStudentComparer : IEqualityComparer<Student>
    {
        public bool Equals(Student x, Student y)
        {
            return x.StudentId.Equals(y.StudentId);
        }

        public int GetHashCode(Student obj)
        {
            return obj.StudentId.GetHashCode();
        }
    }
}