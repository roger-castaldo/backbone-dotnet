using System;
using System.Collections.Generic;
using System.Text;
using Org.Reddragonit.BackBoneDotNet.Interfaces;
using Org.Reddragonit.BackBoneDotNet.Attributes;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Org.Reddragonit.BackBoneDotNet
{
    internal static class DefinitionValidator
    {
        private struct sPathTypePair
        {
            private string _path;
            public string Path
            {
                get { return _path; }
            }

            private Type _modelType;
            public Type ModelType
            {
                get { return _modelType; }
            }

            public sPathTypePair(string path, Type modelType)
            {
                _path = path;
                _modelType = modelType;
            }
        }

        private static Regex _regListPars = new Regex("\\{(\\d+)\\}", RegexOptions.Compiled | RegexOptions.ECMAScript);

        private static bool _IsValidDataActionMethod(MethodInfo method)
        {
            return (method.ReturnType==typeof(bool)) && (method.GetParameters().Length==0);
        }

        /*
         * Called to validate all model definitions through the following checks:
         * 1.  Check to make sure that there is at least 1 route specified for the model.
         * 2.  Check for an empty constructor, and if no empty constructor is specified, ensure that the create method is blocked
         * 3.  Check the all paths specified for the model are unique
         * 4.  Check to make sure only 1 load method exists for a model
         * 5.  Check to make sure only 1 model select load method exists
         * 6.  Check to make sure that the select model method has the right return type
         * 7.  Check to make sure that all properties specified that return an IModel type have a 
         *  Select List method.
         * 8.  Check to make sure a Load method exists
         * 9.  Check to make sure that the id property is not blocked.
         * 10. Check View Attributes to make sure class it not used (use ModelViewClass instead.
         * 11.  Check View Attributes and make sure all names are unique
         * 12.  Check to make sure all paged select lists have proper parameters
         * 13.  Check to make sure all exposed methods are valid (if have same name, have different parameter count)
         */
        internal static List<Exception> Validate(out List<Type> invalidModels)
        {
            List<Exception> errors = new List<Exception>();
            invalidModels = new List<Type>();
            List<sPathTypePair> paths = new List<sPathTypePair>();
            List<Type> models = Utility.LocateTypeInstances(typeof(IModel));
            foreach (Type t in models)
            {
                if (!Utility.IsBlockedModel(t))
                {
                    if (t.GetCustomAttributes(typeof(ModelRoute), false).Length == 0)
                    {
                        invalidModels.Add(t);
                        errors.Add(new NoRouteException(t));
                    }
                    bool genView = true;
                    bool genCollection=true;
                    bool genCollectionView=true;
                    bool genEditForm=true;
                    if (t.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration),false).Length > 0)
                    {
                        ModelBlockJavascriptGeneration mdjg = (ModelBlockJavascriptGeneration)t.GetCustomAttributes(typeof(ModelBlockJavascriptGeneration), false)[0];
                        genView = !(((int)mdjg.BlockType&(int)ModelBlockJavascriptGenerations.View) == (int)ModelBlockJavascriptGenerations.View);
                        genCollection = !(((int)mdjg.BlockType & (int)ModelBlockJavascriptGenerations.Collection) == (int)ModelBlockJavascriptGenerations.Collection);
                        genCollectionView = !(((int)mdjg.BlockType & (int)ModelBlockJavascriptGenerations.CollectionView) == (int)ModelBlockJavascriptGenerations.CollectionView);
                        genEditForm = !(((int)mdjg.BlockType & (int)ModelBlockJavascriptGenerations.EditForm) == (int)ModelBlockJavascriptGenerations.EditForm);
                    }
                    bool hasAdd = false;
                    bool hasUpdate = false;
                    bool hasDelete = false;
                    foreach (MethodInfo mi in t.GetMethods(Constants.STORE_DATA_METHOD_FLAGS))
                    {
                        if (mi.GetCustomAttributes(typeof(ModelSaveMethod), false).Length > 0)
                        {
                            if (hasAdd)
                            {
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new DuplicateModelSaveMethodException(t, mi));
                            }
                            else
                            {
                                hasAdd = true;
                                if (!_IsValidDataActionMethod(mi))
                                {
                                    if (!invalidModels.Contains(t))
                                        invalidModels.Add(t);
                                    errors.Add(new InvalidModelSaveMethodException(t, mi));
                                }
                            }
                        }
                        else if (mi.GetCustomAttributes(typeof(ModelDeleteMethod), false).Length > 0)
                        {
                            if (hasDelete)
                            {
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new DuplicateModelDeleteMethodException(t, mi));
                            }
                            else
                            {
                                hasDelete = true;
                                if (!_IsValidDataActionMethod(mi))
                                {
                                    if (!invalidModels.Contains(t))
                                        invalidModels.Add(t);
                                    errors.Add(new InvalidModelDeleteMethodException(t, mi));
                                }
                            }
                        }
                        else if (mi.GetCustomAttributes(typeof(ModelUpdateMethod), false).Length > 0)
                        {
                            if (hasUpdate)
                            {
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new DuplicateModelUpdateMethodException(t, mi));
                            }
                            else
                            {
                                hasUpdate = true;
                                if (!_IsValidDataActionMethod(mi))
                                {
                                    if (!invalidModels.Contains(t))
                                        invalidModels.Add(t);
                                    errors.Add(new InvalidModelUpdateMethodException(t, mi));
                                }
                            }
                        }
                    }
                    if (hasAdd)
                    {
                        if (t.GetConstructor(Type.EmptyTypes) == null)
                        {
                            invalidModels.Add(t);
                            errors.Add(new NoEmptyConstructorException(t));
                        }
                    }
                    List<string> curAttributes = new List<string>();
                    foreach (ModelViewAttribute mva in t.GetCustomAttributes(typeof(ModelViewAttribute), false))
                    {
                        if (curAttributes.Contains(mva.Name))
                        {
                            invalidModels.Add(t);
                            errors.Add(new RepeatedAttributeTagName(t, mva.Name));
                        }
                        else
                            curAttributes.Add(mva.Name);
                        if (mva.Name.ToUpper() == "CLASS")
                        {
                            invalidModels.Add(t);
                            errors.Add(new InvalidAttributeTagName(t));
                        }
                    }
                    foreach (ModelRoute mr in t.GetCustomAttributes(typeof(ModelRoute), false))
                    {
                        Regex reg = new Regex("^(" + (mr.Host == "*" ? ".+" : mr.Host) + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path) + ")$", RegexOptions.ECMAScript | RegexOptions.Compiled);
                        foreach (sPathTypePair p in paths)
                        {
                            if (reg.IsMatch(p.Path) && (p.ModelType.FullName != t.FullName))
                            {
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new DuplicateRouteException(p.Path, p.ModelType, mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path), t));
                            }
                        }
                        paths.Add(new sPathTypePair(mr.Host + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path), t));
                    }
                    bool found = false;
                    bool foundLoadSelMethod = false;
                    foreach (MethodInfo mi in t.GetMethods(Constants.LOAD_METHOD_FLAGS))
                    {
                        if (mi.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0)
                        {
                            if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                            {
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelListMethodReturnException(t, mi));
                            }
                            if (mi.ReturnType != t)
                            {
                                if (!mi.ReturnType.IsAssignableFrom(t))
                                {
                                    if (!invalidModels.Contains(t))
                                        invalidModels.Add(t);
                                    errors.Add(new InvalidLoadMethodReturnType(t, mi.Name));
                                }
                            }
                            if (mi.ReturnType == t)
                            {
                                if (mi.GetParameters().Length == 1)
                                {
                                    if (mi.GetParameters()[0].ParameterType == typeof(string))
                                    {
                                        if (found)
                                        {
                                            if (!invalidModels.Contains(t))
                                                invalidModels.Add(t);
                                            errors.Add(new DuplicateLoadMethodException(t, mi.Name));
                                        }
                                        found = true;
                                    }
                                }
                            }
                        }
                        if (mi.GetCustomAttributes(typeof(ModelSelectListMethod), false).Length > 0)
                        {
                            if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                            {
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelListMethodReturnException(t, mi));
                            }
                            if (mi.ReturnType.FullName != typeof(sModelSelectOptionValue[]).FullName
                                && mi.ReturnType.FullName != typeof(List<sModelSelectOptionValue>).FullName)
                            {
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelSelectOptionValueReturnException(t, mi));
                            }
                            else if (!mi.IsStatic)
                            {
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelSelectStaticException(t, mi));
                            }
                            else
                            {
                                //    if (foundLoadSelMethod)
                                //    {
                                //        if (!invalidModels.Contains(t))
                                //            invalidModels.Add(t);
                                //        errors.Add(new MultipleSelectOptionValueMethodsException(t, mi));
                                //    }
                                foundLoadSelMethod = true;
                            }
                        }
                        if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                        {
                            Type rtype = mi.ReturnType;
                            if (rtype.FullName.StartsWith("System.Nullable"))
                            {
                                if (rtype.IsGenericType)
                                    rtype = rtype.GetGenericArguments()[0];
                                else
                                    rtype = rtype.GetElementType();
                            }
                            if (rtype.IsArray)
                                rtype = rtype.GetElementType();
                            else if (rtype.IsGenericType)
                            {
                                if (rtype.GetGenericTypeDefinition() == typeof(List<>))
                                    rtype = rtype.GetGenericArguments()[0];
                            }
                            if (rtype != t)
                            {
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelListMethodReturnException(t, mi));
                            }
                            bool isPaged = false;
                            foreach (ModelListMethod mlm in mi.GetCustomAttributes(typeof(ModelListMethod), false))
                            {
                                if (mlm.Paged)
                                {
                                    isPaged = true;
                                    break;
                                }
                            }
                            foreach (ModelListMethod mlm in mi.GetCustomAttributes(typeof(ModelListMethod), false))
                            {
                                MatchCollection mc = _regListPars.Matches(mlm.Path);
                                if (isPaged && !mlm.Paged)
                                {
                                    if (!invalidModels.Contains(t))
                                        invalidModels.Add(t);
                                    errors.Add(new InvalidModelListNotAllPagedException(t, mi, mlm.Path));
                                }
                                if (mc.Count != mi.GetParameters().Length - (isPaged ? 3 : 0))
                                {
                                    if (!invalidModels.Contains(t))
                                        invalidModels.Add(t);
                                    errors.Add(new InvalidModelListParameterCountException(t, mi, mlm.Path));
                                }
                            }
                            ParameterInfo[] pars = mi.GetParameters();
                            for (int x = 0; x < pars.Length; x++)
                            {
                                ParameterInfo pi = pars[x];
                                if (pi.ParameterType.IsGenericType)
                                {
                                    if (pi.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                    {
                                        if (pi.ParameterType.GetGenericArguments()[0].IsGenericType || pi.ParameterType.GetGenericArguments()[0].IsArray)
                                        {
                                            if (!invalidModels.Contains(t))
                                                invalidModels.Add(t);
                                            errors.Add(new InvalidModelListParameterTypeException(t, mi, pi));
                                        }
                                    }
                                    else
                                    {
                                        if (!invalidModels.Contains(t))
                                            invalidModels.Add(t);
                                        errors.Add(new InvalidModelListParameterTypeException(t, mi, pi));
                                    }
                                }
                                else if (pi.ParameterType.IsArray)
                                {
                                    if (!invalidModels.Contains(t))
                                        invalidModels.Add(t);
                                    errors.Add(new InvalidModelListParameterTypeException(t, mi, pi));
                                }
                                if (pi.IsOut && (!isPaged || x != pars.Length - 1))
                                {
                                    if (!invalidModels.Contains(t))
                                        invalidModels.Add(t);
                                    errors.Add(new InvalidModelListParameterOutException(t, mi, pi));
                                }
                                if (isPaged && x >= pars.Length - 3)
                                {
                                    Type ptype = pi.ParameterType;
                                    if (pi.IsOut)
                                        ptype = ptype.GetElementType();
                                    if (ptype != typeof(int)
                                        && ptype != typeof(long)
                                        && ptype != typeof(short)
                                        && ptype != typeof(uint)
                                        && ptype != typeof(ulong)
                                        && ptype != typeof(ushort))
                                    {
                                        if (!invalidModels.Contains(t))
                                            invalidModels.Add(t);
                                        errors.Add(new InvalidModelListPageParameterTypeException(t, mi, pi));
                                    }
                                }
                                if (isPaged && x == pars.Length - 1)
                                {
                                    if (!pi.IsOut)
                                    {
                                        if (!invalidModels.Contains(t))
                                            invalidModels.Add(t);
                                        errors.Add(new InvalidModelListPageTotalPagesNotOutException(t, mi, pi));
                                    }
                                }
                            }
                        }
                    }
                    foreach (PropertyInfo pi in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (pi.GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length == 0)
                        {
                            Type rtype = pi.PropertyType;
                            if (rtype.FullName.StartsWith("System.Nullable"))
                            {
                                if (rtype.IsGenericType)
                                    rtype = rtype.GetGenericArguments()[0];
                                else
                                    rtype = rtype.GetElementType();
                            }
                            if (rtype.IsArray)
                                rtype = rtype.GetElementType();
                            else if (rtype.IsGenericType)
                            {
                                if (rtype.GetGenericTypeDefinition() == typeof(List<>))
                                    rtype = rtype.GetGenericArguments()[0];
                            }
                            if (new List<Type>(rtype.GetInterfaces()).Contains(typeof(IModel)))
                            {
                                bool foundSelMethod = false;
                                foreach (MethodInfo mi in rtype.GetMethods(Constants.LOAD_METHOD_FLAGS))
                                {
                                    if (mi.GetCustomAttributes(typeof(ModelSelectListMethod), false).Length > 0)
                                    {
                                        foundSelMethod = true;
                                        break;
                                    }
                                }
                                if (!foundSelMethod && (hasAdd || hasUpdate) && genEditForm)
                                {
                                    if (!invalidModels.Contains(t))
                                        invalidModels.Add(t);
                                    errors.Add(new NoModelSelectMethodException(t, pi));
                                }
                            }
                        }
                    }
                    if (t.GetProperty("id").GetCustomAttributes(typeof(ModelIgnoreProperty), false).Length > 0)
                    {
                        if (!invalidModels.Contains(t))
                            invalidModels.Add(t);
                        errors.Add(new ModelIDBlockedException(t));
                    }
                    if (!found)
                    {
                        if (!invalidModels.Contains(t))
                            invalidModels.Add(t);
                        errors.Add(new NoLoadMethodException(t));
                    }
                    List<string> methods = new List<string>();
                    foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public|BindingFlags.Instance))
                    {
                        if (mi.GetCustomAttributes(typeof(ExposedMethod), false).Length > 0)
                        {
                            if (methods.Contains(mi.Name + "." + mi.GetParameters().Length.ToString()))
                            {
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new DuplicateMethodSignatureException(t, mi));
                            }
                            else
                                methods.Add(mi.Name + "." + mi.GetParameters().Length.ToString());
                        }
                    }
                }
            }
            return errors;
        }
    }
}
