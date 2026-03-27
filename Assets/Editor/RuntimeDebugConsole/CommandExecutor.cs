using System;
using System.Reflection;
using UnityEngine;
using AAAGame.Debug;

namespace AAAGame.Editor.Debug
{
    /// <summary>
    /// 命令执行器 - 使用反射执行解析后的命令
    /// </summary>
    public static class CommandExecutor
    {
        /// <summary>
        /// 执行命令
        /// </summary>
        public static string Execute(ParsedCommand command)
        {
            try
            {
                // 查找类型
                Type targetType = FindType(command.ClassName);
                if (targetType == null)
                {
                    return $"<color=red>错误: 未找到类型 '{command.ClassName}'</color>";
                }

                // 获取目标对象
                object targetObject = null;
                if (!command.IsStatic)
                {
                    targetObject = GetInstance(targetType);
                    if (targetObject == null)
                    {
                        return $"<color=red>错误: 无法获取 '{command.ClassName}' 的实例</color>";
                    }
                }

                // 执行命令
                if (command.IsProperty)
                {
                    return ExecutePropertyAccess(targetType, targetObject, command);
                }
                else
                {
                    return ExecuteMethodCall(targetType, targetObject, command);
                }
            }
            catch (Exception ex)
            {
                return $"<color=red>执行失败: {ex.Message}\n{ex.StackTrace}</color>";
            }
        }

        /// <summary>
        /// 执行属性访问或赋值
        /// </summary>
        private static string ExecutePropertyAccess(Type targetType, object targetObject, ParsedCommand command)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic;
            flags |= command.IsStatic ? BindingFlags.Static : BindingFlags.Instance;

            // 先尝试属性
            PropertyInfo property = targetType.GetProperty(command.MemberName, flags);
            if (property != null)
            {
                // 属性赋值
                if (!string.IsNullOrEmpty(command.AssignValue))
                {
                    if (!property.CanWrite)
                    {
                        return $"<color=red>错误: 属性 '{command.MemberName}' 是只读的</color>";
                    }

                    object value = TypeConverter.ConvertFromString(command.AssignValue, property.PropertyType);
                    property.SetValue(targetObject, value);
                    return $"<color=green>✓ {command.MemberName} = {value}</color>";
                }
                // 属性读取
                else
                {
                    if (!property.CanRead)
                    {
                        return $"<color=red>错误: 属性 '{command.MemberName}' 是只写的</color>";
                    }

                    object value = property.GetValue(targetObject);
                    return $"<color=cyan>{command.MemberName} = {FormatValue(value)}</color>";
                }
            }

            // 再尝试字段
            FieldInfo field = targetType.GetField(command.MemberName, flags);
            if (field != null)
            {
                // 字段赋值
                if (!string.IsNullOrEmpty(command.AssignValue))
                {
                    object value = TypeConverter.ConvertFromString(command.AssignValue, field.FieldType);
                    field.SetValue(targetObject, value);
                    return $"<color=green>✓ {command.MemberName} = {value}</color>";
                }
                // 字段读取
                else
                {
                    object value = field.GetValue(targetObject);
                    return $"<color=cyan>{command.MemberName} = {FormatValue(value)}</color>";
                }
            }

            return $"<color=red>错误: 未找到属性或字段 '{command.MemberName}'</color>";
        }

        /// <summary>
        /// 执行方法调用
        /// </summary>
        private static string ExecuteMethodCall(Type targetType, object targetObject, ParsedCommand command)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic;
            flags |= command.IsStatic ? BindingFlags.Static : BindingFlags.Instance;

            // 获取所有匹配名称的方法
            MethodInfo[] methods = targetType.GetMethods(flags);
            MethodInfo targetMethod = null;

            foreach (var method in methods)
            {
                if (method.Name == command.MemberName)
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    
                    // 参数数量匹配
                    if (parameters.Length == command.Arguments.Count)
                    {
                        targetMethod = method;
                        break;
                    }
                }
            }

            if (targetMethod == null)
            {
                return $"<color=red>错误: 未找到方法 '{command.MemberName}' (参数数量: {command.Arguments.Count})</color>";
            }

            // 转换参数
            ParameterInfo[] paramInfos = targetMethod.GetParameters();
            object[] args = new object[paramInfos.Length];

            for (int i = 0; i < paramInfos.Length; i++)
            {
                args[i] = TypeConverter.ConvertFromString(command.Arguments[i], paramInfos[i].ParameterType);
            }

            // 调用方法
            object result = targetMethod.Invoke(targetObject, args);

            if (targetMethod.ReturnType == typeof(void))
            {
                return $"<color=green>✓ {command.MemberName}() 执行成功</color>";
            }
            else
            {
                return $"<color=green>✓ {command.MemberName}() = {FormatValue(result)}</color>";
            }
        }

        /// <summary>
        /// 查找类型
        /// </summary>
        private static Type FindType(string typeName)
        {
            // 先在当前程序集中查找
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }

                // 尝试添加常见命名空间
                string[] namespaces = { "AAAGame", "UnityEngine", "UnityGameFramework.Runtime", "GameFramework" };
                foreach (var ns in namespaces)
                {
                    type = assembly.GetType($"{ns}.{typeName}");
                    if (type != null)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        private static object GetInstance(Type type)
        {
            // 尝试 Instance 属性
            PropertyInfo instanceProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceProp != null)
            {
                return instanceProp.GetValue(null);
            }

            // 尝试 FindObjectOfType
            if (type.IsSubclassOf(typeof(MonoBehaviour)))
            {
                return UnityEngine.Object.FindObjectOfType(type);
            }

            return null;
        }

        /// <summary>
        /// 格式化输出值
        /// </summary>
        private static string FormatValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is Vector3 v3)
            {
                return $"({v3.x:F2}, {v3.y:F2}, {v3.z:F2})";
            }

            if (value is Vector2 v2)
            {
                return $"({v2.x:F2}, {v2.y:F2})";
            }

            if (value is Quaternion q)
            {
                return $"({q.x:F2}, {q.y:F2}, {q.z:F2}, {q.w:F2})";
            }

            if (value is Color c)
            {
                return $"({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";
            }

            return value.ToString();
        }
    }
}
