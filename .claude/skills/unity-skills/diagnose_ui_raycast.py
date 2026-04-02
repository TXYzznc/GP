#!/usr/bin/env python3
"""
诊断 UI 系统的射线检测配置
"""
import sys
sys.path.insert(0, 'scripts')

from unity_skills import call_skill

def main():
    print("=" * 100)
    print("🔍 UI 系统诊断 - 拖拽事件检测")
    print("=" * 100)

    # 查找 Canvas
    print("\n1️⃣ 查找所有 Canvas 组件...")
    canvases = call_skill('gameobject_find', component='Canvas', limit=10)

    if canvases and 'gameObjects' in canvases:
        canvas_list = canvases['gameObjects']
        print(f"   找到 {len(canvas_list)} 个 Canvas:")

        for canvas in canvas_list:
            name = canvas.get('name', 'Unknown')
            print(f"\n   📦 {name}")

            # 获取 Canvas 详细信息
            canvas_info = call_skill('gameobject_get_info', name=name)
            if canvas_info and 'components' in canvas_info:
                components = canvas_info.get('components', [])

                # 检查 GraphicRaycaster
                has_raycaster = any('GraphicRaycaster' in c for c in components)
                print(f"      {'✅' if has_raycaster else '❌'} GraphicRaycaster: {has_raycaster}")

                # 检查 Canvas 组件
                for comp in components:
                    if 'Canvas' in comp:
                        print(f"      ✅ Canvas 组件存在")

    # 查找 InventorySlotUI
    print("\n2️⃣ 查找 InventorySlotUI 组件...")
    slots = call_skill('gameobject_find', component='InventorySlotUI', limit=20)

    if slots and 'gameObjects' in slots:
        slot_list = slots['gameObjects']
        print(f"   找到 {len(slot_list)} 个 InventorySlotUI:")

        for i, slot in enumerate(slot_list[:5], 1):
            name = slot.get('name', 'Unknown')
            print(f"\n   {i}. {name}")

            # 获取每个 slot 的详细信息
            slot_info = call_skill('gameobject_get_info', name=name)
            if slot_info:
                components = slot_info.get('components', [])

                # 检查关键组件
                has_drag = any('InventoryDragHandler' in c for c in components)
                has_image = any('Image' in c for c in components)
                has_button = any('Button' in c for c in components)

                print(f"      {'✅' if has_drag else '❌'} InventoryDragHandler: {has_drag}")
                print(f"      {'✅' if has_image else '❌'} Image 组件: {has_image}")
                print(f"      {'✅' if has_button else '❌'} Button 组件: {has_button}")

                # 获取 Image 组件的 RaycastTarget
                if has_image:
                    props = call_skill('component_get_properties', name=name, componentType='Image')
                    if props and 'properties' in props:
                        for prop in props['properties']:
                            if 'raycast' in prop.get('name', '').lower():
                                raycast_enabled = prop.get('value')
                                print(f"      {'✅' if raycast_enabled else '❌'} Image.raycastTarget: {raycast_enabled}")

    # 查找 EventSystem
    print("\n3️⃣ 查找 EventSystem...")
    event_system = call_skill('gameobject_find', component='EventSystem', limit=5)

    if event_system and 'gameObjects' in event_system:
        es_list = event_system['gameObjects']
        if es_list:
            print(f"   ✅ 找到 {len(es_list)} 个 EventSystem (应该有 1 个)")
        else:
            print(f"   ❌ 未找到 EventSystem!")
    else:
        print(f"   ❌ 未找到 EventSystem!")

    # 查找 InventoryDragHandler
    print("\n4️⃣ 查找 InventoryDragHandler 脚本...")
    handlers = call_skill('gameobject_find', component='InventoryDragHandler', limit=20)

    if handlers and 'gameObjects' in handlers:
        handler_list = handlers['gameObjects']
        print(f"   找到 {len(handler_list)} 个 InventoryDragHandler")

        if handler_list:
            # 检查第一个
            first = handler_list[0].get('name', 'Unknown')
            print(f"\n   检查 {first}:")

            handler_info = call_skill('gameobject_get_info', name=first)
            if handler_info:
                components = handler_info.get('components', [])

                # 列出所有组件
                print(f"      组件列表:")
                for comp in components:
                    print(f"        • {comp}")

if __name__ == '__main__':
    main()
