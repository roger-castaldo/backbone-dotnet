using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ExposedMethod : Attribute
    {
        private bool _allowNullResponse;
        public bool AllowNullResponse
        {
            get { return _allowNullResponse; }
        }


        public ExposedMethod() :
            this(false)
        { }

        public ExposedMethod(bool allowNullResponse){
            _allowNullResponse=allowNullResponse;
        }
    }
}
