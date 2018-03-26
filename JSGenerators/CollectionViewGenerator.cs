using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;

namespace Org.Reddragonit.BackBoneDotNet.JSGenerators
{
    /*
     * This generator generates the javascript code for a model CollectionView.
     * The collection object will exist as the path namespace.type.CollectionView
     */
    internal class CollectionViewGenerator : IJSGenerator
    {
        private void _AppendClassName(Type modelType,string host, WrappedStringBuilder sb,bool minimize)
        {
            sb.Append((minimize ? "className:\"" : "\tclassName : \""));
            foreach (string str in ModelNamespace.GetFullNameForModel(modelType, host).Split('.'))
                sb.Append(str + " ");
            foreach (ModelViewClass mvc in modelType.GetCustomAttributes(typeof(ModelViewClass), false))
                sb.Append(mvc.ClassName + " ");
            sb.AppendLine(" CollectionView\",");
        }

        private void _AppendAttributes(Type modelType, WrappedStringBuilder sb, bool minimize)
        {
            if (modelType.GetCustomAttributes(typeof(ModelCollectionViewAttribute), false).Length > 0)
            {
                sb.Append((minimize ? "attributes:{" : "\tattributes: {"));
                object[] atts = modelType.GetCustomAttributes(typeof(ModelCollectionViewAttribute), false);
                for (int x = 0; x < atts.Length; x++)
                    sb.Append((minimize ? "" : "\t\t\"") + ((ModelCollectionViewAttribute)atts[x]).Name + "\" : '" + ((ModelCollectionViewAttribute)atts[x]).Value + "'" + (x < atts.Length - 1 ? "," : ""));
                sb.Append((minimize ? "" : "\t")+"},");
            }
        }

        #region IJSGenerator Members

        public string GenerateJS(Type modelType, string host, List<string> readOnlyProperties, List<string> properties, List<string> viewIgnoreProperties, bool hasUpdate, bool hasAdd, bool hasDelete,bool minimize)
        {
            if (modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false).Length > 0)
            {
                if (((int)((ModelBlockJavascriptGeneration)modelType.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false)[0]).BlockType & (int)ModelBlockJavascriptGenerations.CollectionView) == (int)ModelBlockJavascriptGenerations.CollectionView)
                    return "";
            }
            WrappedStringBuilder sb = new WrappedStringBuilder(minimize);
            sb.AppendFormat((minimize ? 
                "{0}=_.extend(true,{0},{{{1}:Backbone.View.extend({{" 
    : @"//Org.Reddragonit.BackBoneDotNet.JSGenerators.CollectionViewGenerator
{0} = _.extend(true,{0}, {{{1} : Backbone.View.extend({{
"),
            (RequestHandler.UseAppNamespacing ? "App.CollectionViews" : ModelNamespace.GetFullNameForModel(modelType, host)),
            (RequestHandler.UseAppNamespacing ? modelType.Name : "CollectionView"));
            
            string tag = "div";
            if (modelType.GetCustomAttributes(typeof(ModelViewTag), false).Length > 0)
                tag = ((ModelViewTag)modelType.GetCustomAttributes(typeof(ModelViewTag), false)[0]).TagName;
            switch (tag)
            {
                case "tr":
                    sb.AppendLine((minimize ? "tagName:\"table\"," : "\ttagName : \"table\","));
                    break;
                default:
                    sb.AppendLine((minimize ? "tagName:\""+tag+"\"," : "\ttagName : \""+tag+"\","));
                    break;
            }

            _AppendClassName(modelType,host, sb,minimize);
            _AppendAttributes(modelType, sb,minimize);

            sb.AppendLine((minimize ? 
                "render:function(){var el=this.$el;el.html('');"
                :@"  render : function(){
        var el = this.$el;
        el.html('');"));
            if (tag.ToLower() == "tr")
            {
                sb.AppendLine((minimize ?
                    "var thead=$('<thead class=\"'+this.className+' header\"></thead>');el.append(thead);thead.append('<tr></tr>');thead=$(thead.children()[0]);"
                    :@"      var thead = $('<thead class=""'+this.className+' header""></thead>');
        el.append(thead);
        thead.append('<tr></tr>');
        thead = $(thead.children()[0]);"));
                foreach (string str in properties)
                {
                    if (str != "id" && !viewIgnoreProperties.Contains(str))
                        sb.AppendLine((minimize ? "" : "\t\t")+"thead.append('<th className=\"'+this.className+' " + str + "\">" + str + "</th>');");
                }
                sb.AppendLine((minimize ? 
                    "el.append('<tbody></tbody>');el=$(el.children()[1]);"
                    : @"      el.append('<tbody></tbody>');
        el = $(el.children()[1]);"));
            }
            sb.AppendFormat((minimize ? 
                "if(this.collection.length==0){{this.trigger('pre_render_complete',this);this.trigger('render',this);}}else{{var alt=false;for(var x=0;x<this.collection.length;x++){{var vw=new {0}.{1}({{model:this.collection.at(x)}});if(alt){{vw.$el.addClass('Alt');}}alt=!alt;if(x+1==this.collection.length){{vw.on('render',function(){{this.col.trigger('item_render',this.view);this.col.trigger('pre_render_complete',this.col);this.col.trigger('render',this.col);}},{{col:this,view:vw}});}}else{{vw.on('render',function(){{this.col.trigger('item_render',this.view);}},{{col:this,view:vw}});}}el.append(vw.$el);vw.render();}}}}}}}})}});"
                : @"      if(this.collection.length==0){{
            this.trigger('pre_render_complete',this);
            this.trigger('render',this);
        }}else{{
            var alt=false;
            for(var x=0;x<this.collection.length;x++){{
                    var vw = new {0}.{1}({{model:this.collection.at(x)}});
                    if (alt){{
                        vw.$el.addClass('Alt');
                    }}
                    alt=!alt;
                    if(x+1==this.collection.length){{
                        vw.on('render',function(){{this.col.trigger('item_render',this.view);this.col.trigger('pre_render_complete',this.col);this.col.trigger('render',this.col);}},{{col:this,view:vw}});
                    }}else{{
                        vw.on('render',function(){{this.col.trigger('item_render',this.view);}},{{col:this,view:vw}});
                    }}
                    el.append(vw.$el);
                    vw.render();
            }}
        }}
    }}
}})}});"), new object[] { 
            (RequestHandler.UseAppNamespacing ? "App.Views" : ModelNamespace.GetFullNameForModel(modelType, host)),
            (RequestHandler.UseAppNamespacing ? modelType.Name : "View")
        });
            return sb.ToString();
        }

        #endregion
    }
}
