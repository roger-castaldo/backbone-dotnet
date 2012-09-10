using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet
{
    public struct sModelSelectOptionValue
    {
        private string _id;
        public string ID
        {
            get { return _id; }
        }

        private string _text;
        public string Text
        {
            get { return _text; }
        }

        public sModelSelectOptionValue(string id, string text)
        {
            _id = id;
            _text = text;
        }
    }
}
