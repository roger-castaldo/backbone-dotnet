using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    internal class CollectionViewGenerator : IJSGenerator
    {
        private void _AppendClassName(Type modelType,string host, StringBuilder sb)
        {
            sb.Append("\tclassName : \"");
            foreach (string str in ModelNamespace.GetFullNameForModel(modelType, host).Split('.'))
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

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete)
        {
            if (modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false).Length > 0)
            {
                if (((int)((ModelBlockJavascriptGeneration)modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false)[0]).BlockType & (int)ModelBlockJavascriptGenerations.CollectionView) == (int)ModelBlockJavascriptGenerations.CollectionView)
                    return "";
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("//Org.Reddragonit.BackBoneDotNet.JSGenerators.CollectionViewGenerator");
            sb.AppendLine(ModelNamespace.GetFullNameForModel(modelType, host) + " = _.extend(" + ModelNamespace.GetFullNameForModel(modelType, host) + ", {CollectionView : Backbone.View.extend({");
            
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

            _AppendClassName(modelType,host, sb);
            _AppendAttributes(modelType, sb);

            sb.AppendLine("\tinitialize : function(){");
            sb.AppendLine("\t\tthis.collection.on('reset',this.render,this);");
            sb.AppendLine("\t\tthis.collection.on('add',this.render,this);");
            sb.AppendLine("\t\tthis.collection.on('remove',this.render,this);");
            sb.AppendLine("\t},");

            sb.AppendLine("\trender : function(){");
            sb.AppendLine("\t\tvar el = this.$el;");
            sb.AppendLine("\t\tel.html('');");
            if (tag.ToLower() == "tr")
            {
                sb.AppendLine("\t\tvar thead = $('<thead class=\"'+this.className+' header\"></thead>');");
                sb.AppendLine("\t\tel.append(thead);");
                sb.AppendLine("\t\tthead.append('<tr></tr>');");
                sb.AppendLine("\t\tthead = $(thead.children()[0]);");
                foreach (string str in properties)
                {
                    if (str != "id" && !viewIgnoreProperties.Contains(str))
                        sb.AppendLine("\t\tthead.append('<th className=\"'+this.className+' " + str + "\">" + str + "</th>');");
                }
                sb.AppendLine("\t\tel.append('<tbody></tbody>');");
                sb.AppendLine("\t\tel = $(el.children()[0]);");
            }
            sb.AppendLine("\t\tif(this.collection.length==0){");
            sb.AppendLine("\t\t\tthis.trigger('render',this);");
            sb.AppendLine("\t\t}else{");
            sb.AppendLine("\t\t\tvar alt=false;");
            sb.AppendLine("\t\t\tfor(var x=0;x<this.collection.length;x++){");
            sb.AppendLine("\t\t\t\tvar vw = new " + ModelNamespace.GetFullNameForModel(modelType, host) + ".View({model:this.collection.at(x)});");
            sb.AppendLine("\t\t\t\tif (alt){");
            sb.AppendLine("\t\t\t\t\tvw.$el.attr('class',vw.$el.attr('class')+' Alt');");
            sb.AppendLine("\t\t\t\t}");
            sb.AppendLine("\t\t\t\talt=!alt;");
            sb.AppendLine("\t\t\t\tif(x+1==this.collection.length){");
            sb.AppendLine("\t\t\t\t\tvw.on('render',function(){this.col.trigger('item_render',this.view);this.col.trigger('render',this.col);},{col:this,view:vw});");
            sb.AppendLine("\t\t\t\t}else{");
            sb.AppendLine("\t\t\t\t\tvw.on('render',function(){this.col.trigger('item_render',this.view);},{col:this,view:vw});");
            sb.AppendLine("\t\t\t\t}");
            sb.AppendLine("\t\t\t\tel.append(vw.$el);");
            sb.AppendLine("\t\t\t\tvw.render();");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");
            sb.AppendLine("})});");
            return sb.ToString();
        }

        #endregion
    }
}
