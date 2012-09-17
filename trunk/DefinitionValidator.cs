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

        /*
         * Called to validate all model definitions through the following checks:
         * 1.  Check to make sure that there is at least 1 route specified for the model.
         * 2.  Check for an empty constructor, and if no empty constructor is specified, ensure that the create method is blocked
         * 3.  Check the applied model routes for the current model as well as the saved 
         *  routes to ensure that the specified model route is unique.
         * 4.  Check to make sure only 1 load method exists for a model
         * 5.  Check to make sure only 1 model select load method exists
         * 6.  Check to make sure that the select model method has the right return type
         * 7.  Check to make sure that all properties specified that return an IModel type have a 
         *  Select List method.
         * 8.  Check to make sure a Load method exists
         * 9.  Check to make sure that the id property is not blocked.
         * 10. Check View Attributes to make sure class it not used (use ModelViewClass instead.
         * 11.  Check View Attributes and make sure all names are unique
         */
        internal static List<Exception> Validate(out List<Type> invalidModels)
        {
            List<Exception> errors = new List<Exception>();
            invalidModels = new List<Type>();
            List<sPathTypePair> paths = new List<sPathTypePair>();
            List<Type> models = Utility.LocateTypeInstances(typeof(IModel));
            foreach (Type t in models)
            {
                if (t.GetCustomAttributes(typeof(ModelRoute), false).Length == 0)
                {
                    invalidModels.Add(t);
                    errors.Add(new NoRouteException(t));
                }
                if (t.GetConstructor(Type.EmptyTypes) == null)
                {
                    if (t.GetCustomAttributes(typeof(ModelBlockActions), false).Length == 0)
                    {
                        invalidModels.Add(t);
                        errors.Add(new NoEmptyConstructorException(t));
                    }
                    else
                    {
                        if (((int)((ModelBlockActions)t.GetCustomAttributes(typeof(ModelBlockActions), false)[0]).Type & (int)ModelActionTypes.Add) != (int)ModelActionTypes.Add)
                        {
                            invalidModels.Add(t);
                            errors.Add(new NoEmptyConstructorException(t));
                        }
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
                    Regex reg = new Regex((mr.Host == "*" ? ".+" : mr.Host) + (mr.Path.StartsWith("/") ? mr.Path : "/" + mr.Path), RegexOptions.ECMAScript | RegexOptions.Compiled);
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
                foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (mi.GetCustomAttributes(typeof(ModelLoadMethod), false).Length > 0)
                    {
                        if (mi.GetCustomAttributes(typeof(ModelListMethod), false).Length > 0)
                        {
                            if (!invalidModels.Contains(t))
                                invalidModels.Add(t);
                            errors.Add(new InvalidModelListMethodReturnException(t, mi));
                        }
                        if (mi.ReturnType.FullName == t.FullName)
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
                        else
                        {
                            if (foundLoadSelMethod)
                            {
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new MultipleSelectOptionValueMethodsException(t, mi));
                            }
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
                        foreach (ModelListMethod mlm in mi.GetCustomAttributes(typeof(ModelListMethod), false))
                        {
                            MatchCollection mc = _regListPars.Matches(mlm.Path);
                            if (mc.Count != mi.GetParameters().Length)
                            {
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelListParameterCountException(t, mi,mlm.Path));
                            }
                        }
                        foreach (ParameterInfo pi in mi.GetParameters())
                        {
                            if (pi.ParameterType.IsGenericType || pi.ParameterType.IsArray)
                            {
                                if (!invalidModels.Contains(t))
                                    invalidModels.Add(t);
                                errors.Add(new InvalidModelListParameterTypeException(t, mi, pi));
                            }
                        }
                    }
                }
                foreach (PropertyInfo pi in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
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
                        foreach (MethodInfo mi in rtype.GetMethods(BindingFlags.Public | BindingFlags.Static))
                        {
                            if (mi.GetCustomAttributes(typeof(ModelSelectListMethod), false).Length > 0)
                            {
                                foundSelMethod = true;
                                break;
                            }
                        }
                        if (!foundSelMethod)
                        {
                            if (!invalidModels.Contains(t))
                                invalidModels.Add(t);
                            errors.Add(new NoModelSelectMethodException(t, pi));
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
            }
            return errors;
        }
    }
}
