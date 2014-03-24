using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    /*
     * This attribute is used to specify which peices of javascript code should be blocked
     * from being automatically generated.  Those being:
     * Collection 
     *  View 
     *  CollectionView 
     *  EditForm
     */
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
