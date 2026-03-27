#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
超快速路径工具 - 基于配置表，避免慢速文件夹查找
性能：首次访问 ~5ms，后续访问 ~0.1ms (内存缓存)
"""

import json
import os

# 配置文件路径（相对于脚本位置的固定路径）
# 脚本位置：.kiro/path_utils.py
# 项目根目录：.kiro 的上级目录
# 从 .kiro 目录向上 1 级到达项目根目录
CONFIG_FILE = os.path.join(
    os.path.dirname(__file__),
    'project_paths.json'
)

# 全局路径缓存
_path_cache = None

def load_project_paths():
    """
    加载项目路径配置（带内存缓存）
    
    返回:
        dict: 路径配置字典，失败返回None
    """
    global _path_cache
    
    # 如果已缓存，直接返回
    if _path_cache is not None:
        return _path_cache
    
    try:
        with open(CONFIG_FILE, 'r', encoding='utf-8') as f:
            _path_cache = json.load(f)
        return _path_cache
    except FileNotFoundError:
        print(f"[ERROR] 路径配置文件不存在: {CONFIG_FILE}")
        print(f"[INFO] 请运行: python init_project_paths.py")
        return None
    except json.JSONDecodeError as e:
        print(f"[ERROR] 路径配置文件格式错误: {e}")
        return None
    except Exception as e:
        print(f"[ERROR] 无法加载路径配置: {e}")
        return None

def get_path(path_key):
    """
    获取指定路径（O(1)时间复杂度）
    
    参数:
        path_key: 路径键名
    
    返回:
        str: 路径字符串，失败返回None
    """
    config = load_project_paths()
    if config and 'paths' in config:
        return config['paths'].get(path_key)
    return None

def get_project_root():
    """获取项目根目录"""
    config = load_project_paths()
    return config.get('project_root') if config else None

def get_mcp_workspace_path():
    """获取AI工作区路径"""
    return get_path('AI_workspace')

# 常用路径快捷函数
def get_assets_path():
    """获取Assets目录路径"""
    return get_path('assets')

def get_data_tables_path():
    """获取数据表目录路径"""
    return get_path('data_tables')

def get_game_data_path():
    """获取游戏数据目录路径"""
    return get_path('game_data')

def get_scripts_path():
    """获取脚本目录路径"""
    return get_path('scripts')

def get_ab_working_path():
    """获取AB工作目录路径"""
    return get_path('ab_working')

def get_knowledge_base_path():
    """获取知识库目录路径"""
    return get_path('knowledge_base')

def get_kiro_config_path():
    """获取Kiro配置目录路径"""
    return get_path('kiro_config')

# 兼容性函数（保持向后兼容）
def find_project_root_smart():
    """
    智能项目根目录查找（兼容性函数）
    现在直接从配置表读取，极速返回
    """
    return get_project_root()

def reload_config():
    """重新加载配置（清除缓存）"""
    global _path_cache
    _path_cache = None
    return load_project_paths()

def verify_paths():
    """验证所有配置的路径是否存在"""
    config = load_project_paths()
    if not config:
        return False
    
    print("📋 路径验证结果:")
    all_exist = True
    
    for key, path in config['paths'].items():
        exists = os.path.exists(path)
        status = "✅" if exists else "❌"
        print(f"  {key:20} {status} {path}")
        if not exists:
            all_exist = False
    
    return all_exist

def get_config_info():
    """获取配置信息"""
    config = load_project_paths()
    if config:
        return {
            'version': config.get('version', 'unknown'),
            'last_updated': config.get('last_updated', 'unknown'),
            'config_file': CONFIG_FILE,
            'paths_count': len(config.get('paths', {}))
        }
    return None

# 使用示例和测试
if __name__ == "__main__":
    print("=" * 60)
    print("超快速路径工具测试")
    print("=" * 60)
    
    # 显示配置信息
    info = get_config_info()
    if info:
        print(f"配置版本: {info['version']}")
        print(f"更新时间: {info['last_updated']}")
        print(f"配置文件: {info['config_file']}")
        print(f"路径数量: {info['paths_count']}")
        print()
    
    # 测试常用路径
    test_paths = [
        ('项目根目录', get_project_root()),
        ('AI工作区', get_mcp_workspace_path()),
        ('Assets目录', get_assets_path()),
        ('数据表目录', get_data_tables_path()),
        ('脚本目录', get_scripts_path()),
        ('知识库目录', get_knowledge_base_path())
    ]
    
    print("🚀 路径测试结果:")
    for name, path in test_paths:
        if path:
            exists = "✅" if os.path.exists(path) else "❌"
            print(f"  {name:12} {exists} {path}")
        else:
            print(f"  {name:12} ❌ 配置未找到")
    
    print("\n" + "=" * 60)