using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
