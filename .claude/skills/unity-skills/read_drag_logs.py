#!/usr/bin/env python3
"""
读取 Unity 控制台日志，查找拖拽问题
"""
import sys
import json

# 添加脚本路径
sys.path.insert(0, 'scripts')

try:
    from unity_skills import call_skill, is_unity_running
except ImportError as e:
    print(f"导入失败: {e}")
    print("确保 Unity 编辑器正在运行并启动了 UnitySkills 服务器")
    sys.exit(1)

def main():
    # 检查 Unity 是否运行
    if not is_unity_running():
        print("❌ Unity 编辑器未运行或服务器未启动")
        print("请在 Unity 中执行: Window > UnitySkills > Start Server")
        return

    print("✅ Unity 已连接\n")

    # 读取所有日志
    print("=" * 80)
    print("📋 读取所有控制台日志 (All)")
    print("=" * 80)
    result = call_skill('console_get_logs', type='All', limit=100)

    if result and 'logs' in result:
        logs = result['logs']
        print(f"总共找到 {len(logs)} 条日志\n")

        # 过滤拖拽相关的日志
        drag_logs = [log for log in logs if 'drag' in str(log).lower() or 'DragHandler' in str(log)]

        if drag_logs:
            print(f"🎯 找到 {len(drag_logs)} 条拖拽相关的日志:\n")
            for i, log in enumerate(drag_logs, 1):
                print(f"[{i}] {log}")
                print("-" * 80)
        else:
            print("⚠️ 未找到拖拽相关的日志")

        # 显示最近的错误和警告
        print("\n" + "=" * 80)
        print("⚠️ 错误和警告日志")
        print("=" * 80)
        error_logs = call_skill('console_get_logs', type='Error', limit=50)
        warning_logs = call_skill('console_get_logs', type='Warning', limit=50)

        if error_logs and 'logs' in error_logs:
            for log in error_logs['logs']:
                print(f"❌ ERROR: {log}")

        if warning_logs and 'logs' in warning_logs:
            for log in warning_logs['logs']:
                print(f"⚠️ WARNING: {log}")

        # 获取日志统计
        print("\n" + "=" * 80)
        print("📊 日志统计")
        print("=" * 80)
        stats = call_skill('console_get_stats')
        if stats:
            print(json.dumps(stats, indent=2, ensure_ascii=False))
    else:
        print(f"无法读取日志: {result}")

if __name__ == '__main__':
    main()
