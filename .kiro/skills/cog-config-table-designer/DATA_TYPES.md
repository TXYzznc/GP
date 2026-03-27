# 支持的数据类型完整列表

## 基础数值类型
- `int`, `int32`, `system.int32` - 32位有符号整数
- `uint`, `system.uint32` - 32位无符号整数  
- `short`, `system.int16` - 16位有符号整数
- `ushort`, `system.uint16` - 16位无符号整数
- `long`, `system.int64` - 64位有符号整数
- `ulong`, `system.uint64` - 64位无符号整数
- `byte`, `system.byte` - 8位无符号整数
- `sbyte`, `system.sbyte` - 8位有符号整数

## 浮点数类型
- `float`, `system.single` - 单精度浮点数
- `double`, `system.double` - 双精度浮点数
- `decimal`, `system.decimal` - 高精度小数

## 字符串和字符类型
- `string`, `system.string` - 字符串
- `char`, `system.char` - 单个字符

## 布尔类型
- `bool`, `system.boolean` - 布尔值（true/false）

## 日期时间类型
- `datetime`, `system.datetime` - 日期时间（格式：yyyy-MM-dd HH:mm:ss）

## Unity特有类型
- `vector2`, `unityengine.vector2` - 2D向量（格式：x,y）
- `vector2int`, `unityengine.vector2int` - 2D整数向量
- `vector3`, `unityengine.vector3` - 3D向量（格式：x,y,z）
- `vector3int`, `unityengine.vector3int` - 3D整数向量
- `vector4`, `unityengine.vector4` - 4D向量（格式：x,y,z,w）
- `quaternion`, `unityengine.quaternion` - 四元数（格式：x,y,z,w）
- `color`, `unityengine.color` - 颜色（格式：r,g,b,a）
- `color32`, `unityengine.color32` - 32位颜色
- `rect`, `unityengine.rect` - 矩形（格式：x,y,width,height）
- `int4` - Unity Mathematics 4D整数向量

## 枚举类型
- `enum`, `system.enum` - 枚举值（格式：EnumType.Value）

## 数组类型
- `int[]`, `int32[]`, `system.int32[]` - 整数数组（格式：1,2,3,4）
- `uint[]` - 无符号整数数组
- `float[]` - 浮点数数组
- `double[]` - 双精度浮点数数组
- `bool[]` - 布尔数组
- `string[]` - 字符串数组
- `long[]` - 长整数数组
- `vector2[]` - 2D向量数组
- `vector2int[]` - 2D整数向量数组
- `vector3[]` - 3D向量数组
- `vector3int[]` - 3D整数向量数组
- `vector4[]` - 4D向量数组
- `int4[]` - 4D整数向量数组

## 二维数组类型
- `int[,]` - 整数二维数组（格式：1,2|3,4）
- `bool[,]` - 布尔二维数组
- `double[,]` - 双精度浮点数二维数组
- `float[,]` - 浮点数二维数组

## 特殊类型
- `id` - ID类型（特殊的整数类型）
- `comment` - 注释类型
- `type` - 类型字段
