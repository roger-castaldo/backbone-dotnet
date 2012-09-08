using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Interfaces
{
    public interface IModel
    {
        string id { get; }
        bool Delete();
        bool Update();
        bool Save();
    }
}
