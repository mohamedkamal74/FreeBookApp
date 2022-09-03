using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domin.Entity
{
    public class Helper
    {
        public const string PathImageUser = "/Images/Users";
        public const string PathSaveImageUser = "Images/Users";

        public const string MsgType = "msgType";
        public const string Title = "title";
        public const string Msg = "msg";

        public const string Success = "success";
        public const string Error = "error";


        public const string Admin = "Admin";

        public enum ECurrentState
        {
            Active=1,
            Delete=0
        }
    }
}
