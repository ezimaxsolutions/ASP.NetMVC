//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace EDS
{
    using System;
    using System.Collections.Generic;
    
    public partial class tblGender
    {
        public tblGender()
        {
            this.tblStudents = new HashSet<tblStudent>();
        }
    
        public int GenderId { get; set; }
        public string GenderCode { get; set; }
        public string GenderDesc { get; set; }
        public System.DateTime CreateDatetime { get; set; }
        public Nullable<System.DateTime> ChangeDatetime { get; set; }
    
        public virtual ICollection<tblStudent> tblStudents { get; set; }
    }
}
