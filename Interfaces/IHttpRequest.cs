using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;

namespace Org.Reddragonit.BackBoneDotNet.Interfaces
{
    /*
     * This interface is used to give the handler access to a generic http request object.
     * It was done this way to allow this library to be used with any sort of web server,
     * All that needs to be implemented is the methods/properties specified.
     */
    public interface IHttpRequest
    {
        //The url of the request
        Uri URL { get; }
        //the method call 
        string Method { get; }
        //The content (either Query String, or Post Content) passed to the request
        string ParameterContent { get; }
        //Called to set the response content type
        void SetResponseContentType(string type);
        //Called to write content to the underlying http request
        void WriteContent(string content);
        //Called to Send the completed request response
        void SendResponse();
        //Called to set the Response Status Number
        void SetResponseStatus(int statusNumber);
        //Returns the Accept Language Header value for error language detection
        string AcceptLanguageHeaderValue { get; }
        //Returns any additional Backbone variables for the request, if needed
        Hashtable AdditionalBackboneVariables { get; }
        /*The functions below are used to test for security calls for each of the function type, a model 
         * is supplied for non-static calls as well as the submitted parameters and ids for static ones
        */
        bool IsLoadAllowed(Type model,string id, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsLoadAllAllowed(Type model, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsListAllowed(Type model,MethodInfo method, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsSelectAllowed(Type model, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsUpdateAllowed(IModel model, Hashtable parameters, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsSaveAllowed(Type model, Hashtable parameters, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsDeleteAllowed(Type model, string id, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsJsURLAllowed(string url, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsExposedMethodAllowed(IModel model, string methodName, Hashtable parameters, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsStaticExposedMethodAllowed(Type model, string methodName, Hashtable parameters, out int HttpStatusCode, out string HttpStatusMessage);
    }
}
