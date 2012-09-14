using System;
using System.Collections.Generic;
using System.Text;

namespace Org.Reddragonit.BackBoneDotNet.Attributes
{
    public enum ModelViewTagTypes
    {
        ul,
        ol,
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

    public enum ModelBlockJavascriptGenerations
    {
        Collection = 1,
        View = 2,
        CollectionView = 4
    }
}
