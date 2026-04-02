#!/usr/bin/env python3
"""
读取 Unity 控制台日志，提取完整的拖拽相关消息
"""
import sys
import re

sys.path.insert(0, 'scripts')

try:
    from unity_skills import call_skill
except ImportError as e:
    print(f"导入失败: {e}")
    sys.exit(1)

def main():
    print("=" * 100)
    print("📋 读取 Unity 控制台日志 - 拖拽问题诊断")
    print("=" * 100)

    # 读取所有日志
    result = call_skill('console_get_logs', type='All', limit=150)

    if not result or 'logs' not in result:
        print(f"无法读取日志: {result}")
        return

    logs = result['logs']

    # 提取拖拽相关日志
    drag_logs = []
    for log in logs:
        msg = str(log.get('message', ''))
        if 'DragHandler' in msg or 'drag' in msg.lower():
            drag_logs.append(log)

    print(f"\n✅ 找到 {len(drag_logs)} 条拖拽相关日志\n")

    # 分类和显示
    for i, log in enumerate(drag_logs, 1):
        msg = log.get('message', '')
        log_type = log.get('type', 'Unknown')

        # 提取第一行内容
        first_line = msg.split('\n')[0] if msg else ''

        # 提取方法名
        method_match = re.search(r'(\w+)\s*\(\s*\)', msg)
        method_name = method_match.group(1) if method_match else 'Unknown'

        # 确定日志优先级
        if log_type == 'Error':
            prefix = '❌'
        elif log_type == 'Warning':
            prefix = '⚠️'
        else:
            prefix = '📌'

        print(f"[{i:2d}] {prefix} {method_name:30s} | {first_line[:60]}")

    # 检查是否有错误或警告
    print("\n" + "=" * 100)
    print("⚠️ 错误和警告")
    print("=" * 100)

    errors = [log for log in logs if log.get('type') == 'Error']
    warnings = [log for log in logs if log.get('type') == 'Warning']

    if errors:
        print(f"\n❌ 找到 {len(errors)} 条错误:\n")
        for err in errors:
            print(f"  • {err.get('message', '')[:100]}")
    else:
        print("✅ 没有错误")

    if warnings:
        print(f"\n⚠️ 找到 {len(warnings)} 条警告:\n")
        for warn in warnings:
            print(f"  • {warn.get('message', '')[:100]}")
    else:
        print("✅ 没有警告")

    # 分析拖拽流程
    print("\n" + "=" * 100)
    print("🔍 拖拽流程分析")
    print("=" * 100)

    drag_methods = {}
    for log in drag_logs:
        msg = str(log.get('message', ''))
        method_match = re.search(r'(\w+)\s*\(\s*\)', msg)
        method_name = method_match.group(1) if method_match else 'Unknown'

        if method_name not in drag_methods:
            drag_methods[method_name] = 0
        drag_methods[method_name] += 1

    print("\n📊 方法调用次数:\n")
    for method, count in sorted(drag_methods.items(), key=lambda x: -x[1]):
        print(f"  {method:30s}: {count:3d} 次")

    # 判断流程完整性
    print("\n" + "=" * 100)
    print("✓ 流程检查")
    print("=" * 100)

    has_begin = 'OnBeginDrag' in drag_methods
    has_drag = 'OnDrag' in drag_methods
    has_end = 'OnEndDrag' in drag_methods
    has_target = 'GetTargetSlot' in drag_methods
    has_handle = 'HandleDrop' in drag_methods

    print(f"\n{'✅' if has_begin else '❌'} OnBeginDrag 调用: {drag_methods.get('OnBeginDrag', 0)} 次")
    print(f"{'✅' if has_drag else '❌'} OnDrag 调用: {drag_methods.get('OnDrag', 0)} 次")
    print(f"{'✅' if has_end else '❌'} OnEndDrag 调用: {drag_methods.get('OnEndDrag', 0)} 次")
    print(f"{'✅' if has_target else '❌'} GetTargetSlot 调用: {drag_methods.get('GetTargetSlot', 0)} 次")
    print(f"{'✅' if has_handle else '❌'} HandleDrop 调用: {drag_methods.get('HandleDrop', 0)} 次")

    # 问题诊断
    print("\n" + "=" * 100)
    print("🐛 问题诊断")
    print("=" * 100)

    if not has_begin:
        print("\n⚠️ OnBeginDrag 没有被调用 → 拖拽事件没有触发")
        print("   检查: Canvas 的 GraphicRaycaster 组件")
    elif has_begin and not has_target:
        print("\n⚠️ OnEndDrag 中没有调用 GetTargetSlot → 目标检测失败")
    elif has_target and not has_handle:
        print("\n⚠️ 虽然找到了目标，但没有执行 HandleDrop → 可能是目标格子验证失败")
    elif has_handle:
        print("\n✅ 完整的拖拽流程已执行")
        print("   问题可能在于拖拽逻辑本身，检查 HandleDrop 的实现")

if __name__ == '__main__':
    main()
