using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace wmibrowser
{
    public class PropDataBind
    {
        string propName;
        string propValue;
        string propType;

        public string PropType
        {
            get
            {
                return propType;
            }
            set
            {
                this.propType = value;
            }
        }

        public string PropName
        {
            get
            {
                return propName;
            }
            set
            {
                this.propName = value;
            }
        }

        public string PropValue
        {
            get
            {
                return propValue;
            }
            set
            {
                this.propValue = value;
            }
        }
    }

    public class MethodDataBind
    {
        string methodName;

        public string MethodName
        {
            get
            {
                return methodName;
            }
            set
            {
                this.methodName = value;
            }
        }
    }

    public static class Queries
    {
        public readonly static ObjectQuery Namespaces = new ObjectQuery("SELECT Name FROM __Namespace");
        public readonly static ObjectQuery Classes = new ObjectQuery("SELECT * FROM meta_class");
    }

    public static class Config
    {
        public readonly static char[] Banned =
        {
            '"', '\'', '/', '\\', '!', ',', ';'
        };

        public static bool WhitelistValidate(string message)
        {
            for(int i = 0; i < Banned.Length; i++)
            {
                if (message.Contains(Banned[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static ConnectionOptions Connect = new ConnectionOptions()
        {
            Authentication = AuthenticationLevel.PacketPrivacy,
            Impersonation = ImpersonationLevel.Impersonate,
            Timeout = TimeSpan.FromSeconds(5)
        };

        public static ObjectGetOptions Get = new ObjectGetOptions()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        public static EnumerationOptions Enum = new EnumerationOptions()
        {
            BlockSize = 512,
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public static class Logger
    {
        public static List<LMessage> LoggedMessages = new List<LMessage>();

        public static void LogMessage(string message) => LoggedMessages.Add(new LMessage(message));

        public class LMessage
        {
            string Message;
            DateTime DateTime;

            public LMessage(string message)
            {
                Message = message;
                DateTime = DateTime.Now;
            }
        }
    }
}
