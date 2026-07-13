using plog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polarite.Debugging
{
    public static class Logs
    {
        public static Dictionary<object, Logger> ObjLoggers = new Dictionary<object, Logger>();
        public static Dictionary<object, Logger> SpecialLoggers = new Dictionary<object, Logger>();
        public static Logger GetLogger(object context)
        {
            if(context == null)
            {
                return new Logger("Polarite");
            }
            if (ObjLoggers.TryGetValue(context, out var logger))
            {
                return logger;
            }
            else
            {
                logger = new Logger(context.GetType().Name);
                ObjLoggers[context] = logger;
                return logger;
            }
        }
        public static Logger GetLogger(string name)
        {
            if(string.IsNullOrEmpty(name))
            {
                return new Logger("Polarite");
            }
            if (SpecialLoggers.TryGetValue(name, out var logger))
            {
                return logger;
            }
            else
            {
                logger = new Logger(name);
                SpecialLoggers[name] = logger;
                return logger;
            }
        }
        public static void Info(string str, object context = null, string name = "")
        {
            Logger logger = string.IsNullOrEmpty(name) ? GetLogger(context) : GetLogger(name);
            logger.Info(str, context: context);
            ItePlugin.log.LogInfo(str);
        }
        public static void Warn(string str, object context = null, string name = "")
        {
            Logger logger = string.IsNullOrEmpty(name) ? GetLogger(context) : GetLogger(name);
            logger.Warning(str, context: context);
            ItePlugin.log.LogWarning(str);
        }
        public static void Error(string str, object context = null, string name = "")
        {
            Logger logger = string.IsNullOrEmpty(name) ? GetLogger(context) : GetLogger(name);
            logger.Error(str, context: context);
            ItePlugin.log.LogError(str);
        }
        public static void DebugError(string str, object context = null, string name = "")
        {
            if (!ItePlugin.logDebugErrorLogs.value)
            {
                return;
            }
            Logger logger = string.IsNullOrEmpty(name) ? GetLogger(context) : GetLogger(name);
            logger.Error(str, context: context);
            ItePlugin.log.LogError(str);
        }
        public static void Fine(string str, object context = null, string name = "")
        {
            Logger logger = string.IsNullOrEmpty(name) ? GetLogger(context) : GetLogger(name);
            logger.Fine(str, context: context);
        }
        public static void Debug(string str, object context = null, string name = "")
        {
            if(!ItePlugin.logDebugLogs.value)
            {
                return;
            }
            Logger logger = string.IsNullOrEmpty(name) ? GetLogger(context) : GetLogger(name);
            logger.Debug(str, context: context);
            ItePlugin.log.LogDebug(str);
        }
    }
}
