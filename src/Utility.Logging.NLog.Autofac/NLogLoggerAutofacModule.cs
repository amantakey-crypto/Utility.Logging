using System.Linq;
using Autofac;
using Autofac.Core;
using System;
using System.Collections.Generic;

namespace Utility.Logging.NLog.Autofac
{
    public class NLogLoggerAutofacModule : Module
    {
        protected override void AttachToComponentRegistration(IComponentRegistry registry, IComponentRegistration registration)
        {
            var type = registration.Activator.LimitType;
            if (HasConstructorDependencyOnLogger(type))
            {
                registration.Preparing += (s, e) =>
                {
                    e.Parameters = new[] { loggerParameter }.Concat(e.Parameters);
                };
            }

            var properties = GetLoggerProperties(type).ToList();

            if (properties.Count > 0)
            {
                registration.Activated += (s, e) =>
                {
                    var logger = e.Context.Resolve<ILoggerFactory>().GetLogger(type);
                    foreach (var prop in properties)
                    {
                        prop.SetValue(e.Instance, logger, System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance, null, null, null);
                    }
                };
            }

        }

        private IEnumerable<System.Reflection.PropertyInfo> GetLoggerProperties(Type type)
        {
            return type.GetProperties().Where(property => property.CanWrite && property.PropertyType == typeof(ILogger));
        }

        private bool HasConstructorDependencyOnLogger(Type type)
        {
            return type.GetConstructors()
            .Any(ctor => ctor.GetParameters()
            .Any(parameter => parameter.ParameterType == typeof(ILogger)));
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<NLogLoggerFactory>()
              .As<ILoggerFactory>()
              .SingleInstance();
        }

        private readonly Parameter loggerParameter =
          new ResolvedParameter((p, c) => p.ParameterType == typeof(ILogger),
                                (p, c) => c.Resolve<ILoggerFactory>().GetLogger(p.Member.DeclaringType));
    }
}