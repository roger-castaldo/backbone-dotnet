using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet
{
    /*
     * Used to provide a Select Option for editing and adding models when the 
     * model property is another model, return through Select methods.
     */
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
