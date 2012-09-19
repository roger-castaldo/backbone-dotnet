using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    internal class CollectionViewGenerator : IJSGenerator
    {
        private void _AppendClassName(Type modelType, StringBuilder sb)
        {
            sb.Append("\tclassName : \"");
            foreach (string str in modelType.FullName.Split('.'))
                sb.Append(str + " ");
            foreach (ModelViewClass mvc in modelType.GetCustomAttributes(typeof(ModelViewClass), false))
                sb.Append(mvc.ClassName + " ");
            sb.AppendLine(" CollectionView\",");
        }

        private void _AppendAttributes(Type modelType, StringBuilder sb)
        {
            if (modelType.GetCustomAttributes(typeof(ModelViewAttribute), false).Length > 0)
            {
                sb.Append("\tattributes: {");
                object[] atts = modelType.GetCustomAttributes(typeof(ModelViewAttribute), false);
                for (int x = 0; x < atts.Length; x++)
                    sb.Append("\t\t\"" + ((ModelViewAttribute)atts[x]).Name + "\" : '" + ((ModelViewAttribute)atts[x]).Value + "'" + (x < atts.Length - 1 ? "," : ""));
                sb.Append("\t},");
            }
        }

        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties)
        {
            if (modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false).Length > 0)
            {
                if (((int)((ModelBlockJavascriptGeneration)modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false)[0]).BlockType & (int)ModelBlockJavascriptGenerations.CollectionView) == (int)ModelBlockJavascriptGenerations.CollectionView)
                    return "";
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Org.Reddragonit.BackBoneDotNet.JSGenerators.CollectionViewGenerator");
            sb.AppendLine(modelType.FullName + ".CollectionView = Backbone.View.extend({");
            
            string tag = "div";
            if (modelType.GetCustomAttributes(typeof(ModelViewTag), false).Length > 0)
                tag = ((ModelViewTag)modelType.GetCustomAttributes(typeof(ModelViewTag), false)[0]).TagName;
            switch (tag)
            {
                case "tr":
                    sb.AppendLine("\ttagName : \"table\",");
                    break;
                default:
                    sb.AppendLine("\ttagName : \""+tag+"\",");
                    break;
            }

            _AppendClassName(modelType, sb);
            _AppendAttributes(modelType, sb);

            sb.AppendLine("\tinitialize : function(){");
            sb.AppendLine("\t\tthis.collection.on('reset',this.render,this);");
            sb.AppendLine("\t\tthis.collection.on('add',this.render,this);");
            sb.AppendLine("\t\tthis.collection.on('remove',this.render,this);");
            sb.AppendLine("\t},");

            sb.AppendLine("\trender : function(){");
            sb.AppendLine("\t\tthis.$el.html('');");
            sb.AppendLine("\t\tvar alt=false;");
            sb.AppendLine("\t\tfor(var x=0;x<this.collection.length;x++){");
            sb.AppendLine("\t\t\tvar vw = new " + modelType.FullName + ".View({model:this.collection.at(x)});");
            sb.AppendLine("\t\t\tif (alt){");
            sb.AppendLine("\t\t\t\tvw.$el.attr('class',vw.$el.attr('class')+' Alt');");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t\talt=!alt;");
            sb.AppendLine("\t\t\tthis.$el.append(vw.$el);");
            sb.AppendLine("\t\t\tvw.render();");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");
            sb.AppendLine("});");
            return sb.ToString();
        }

        #endregion
    }
}
