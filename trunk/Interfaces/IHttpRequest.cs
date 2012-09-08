using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Interfaces
{
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
