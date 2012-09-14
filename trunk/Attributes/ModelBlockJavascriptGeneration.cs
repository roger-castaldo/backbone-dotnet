using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=false)]
    public class ModelBlockJavascriptGeneration : Attribute
    {
        private ModelBlockJavascriptGenerations _blockType;
        public ModelBlockJavascriptGenerations BlockType
        {
            get { return _blockType; }
        }

        public ModelBlockJavascriptGeneration(ModelBlockJavascriptGenerations blockType)
        {
            _blockType = blockType;
        }
    }
}
