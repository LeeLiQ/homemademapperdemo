using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace mapper_demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    public static class Mapper<TSource, TTarget>
        where TSource : class
        where TTarget : class
    {
        public readonly static Func<TSource, TTarget> Map;

        static Mapper()
        {
            if (Map == null)
                Map = GetMap();
        }

        private static Func<TSource, TTarget> GetMap()
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);

            var parameterExpression = Expression.Parameter(sourceType, "p");
            var memberInitExpression = GetExpression(parameterExpression, sourceType, targetType);
            var lambda = Expression.Lambda<Func<TSource, TTarget>>(memberInitExpression, parameterExpression);
            return lambda.Compile();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterExpression"></param>
        /// <param name="sourceType"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        private static MemberInitExpression GetExpression(Expression parameterExpression, Type sourceType, Type targetType)
        {
            var memberBindings = new List<MemberBinding>();
            foreach (var targetItem in targetType.GetProperties().Where(x => x.PropertyType.IsPublic && x.CanWrite))
            {
                var sourceItem = sourceType.GetProperty(targetItem.Name);

                // check if property can read/write.
                if (sourceItem == null || !sourceItem.CanRead || sourceItem.PropertyType.IsNotPublic)
                    continue;
                // ignore NotMapped properties
                if (sourceItem.GetCustomAttribute<NotMappedAttribute>() != null)
                    continue;

                var propertyExpression = Expression.Property(parameterExpression, sourceItem);

                // check if property is class and has different types
                if (targetItem.PropertyType.IsClass && sourceItem.PropertyType.IsClass && targetItem.PropertyType != sourceItem.PropertyType)
                {
                    // prevent self-reference and infinite recursion
                    if (targetItem.PropertyType != targetType)
                    {
                        var memberInit = GetExpression(propertyExpression, sourceItem.PropertyType, targetItem.PropertyType);
                        memberBindings.Add(Expression.Bind(targetItem, memberInit));
                        continue;
                    }
                }
                if (targetItem.PropertyType != sourceItem.PropertyType)
                    continue;
                memberBindings.Add(Expression.Bind(targetItem, propertyExpression));
            }
            return Expression.MemberInit(Expression.New(targetType), memberBindings);
        }
    }
}
