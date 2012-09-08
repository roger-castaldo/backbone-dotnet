using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    public enum ModelViewTagTypes
    {
        li,
        tr,
        div
    }

    public enum ModelActionTypes
    {
        Add = 1,
        Edit=2,
        Delete = 4
    }

    public enum ModelEditAddTypes
    {
        inline,
        dialog
    }
}
