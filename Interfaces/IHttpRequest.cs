using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Org.Reddragonit.BackBoneDotNet.Interfaces
{
    /*
     * This interface is used to give the handler access to a generic http request object.
     * It was done this way to allow this library to be used with any sort of web server,
     * All that needs to be implemented is the methods/properties specified.
     */
    public interface IHttpRequest
    {
        Uri URL { get; }
        string Method { get; }
        string ParameterContent { get; }
        void SetResponseContentType(string type);
        void WriteContent(string content);
        void SendResponse();
        void SetResponseStatus(int statusNumber);
        string AcceptLanguageHeaderValue { get; }
        Hashtable AdditionalBackboneVariables { get; }
        bool IsLoadAllowed(Type model,string id, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsLoadAllAllowed(Type model, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsListAllowed(Type model, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsSelectAllowed(Type model, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsUpdateAllowed(IModel model, Hashtable parameters, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsSaveAllowed(Type model, Hashtable parameters, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsDeleteAllowed(Type model, string id, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsJsURLAllowed(string url, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsExposedMethodAllowed(IModel model, string methodName, Hashtable parameters, out int HttpStatusCode, out string HttpStatusMessage);
        bool IsStaticExposedMethodAllowed(Type model, string methodName, Hashtable parameters, out int HttpStatusCode, out string HttpStatusMessage);
    }
}
