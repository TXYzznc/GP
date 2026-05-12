#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
将所有 Mermaid .md 文件转换为 Graphviz DOT 格式
优化参数：正交线条 + 最大空间利用率
"""

import os
import re
from pathlib import Path
from typing import Optional, List, Dict, Tuple

class MermaidToDotConverter:
    """Mermaid 到 DOT 格式转换器"""

    def __init__(self):
        self.mermaid_type = None
        self.nodes = {}
        self.edges = []
        self.subgraphs = {}

    def extract_mermaid_code(self, file_path: str) -> Optional[str]:
        """从 .md 文件中提取 Mermaid 代码"""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()

            # 提取 ```mermaid 代码块
            match = re.search(r'```mermaid\n(.*?)```', content, re.DOTALL)
            if match:
                return match.group(1).strip()
            return None
        except Exception as e:
            print(f"[ERROR] 读取文件失败: {e}")
            return None

    def detect_diagram_type(self, mermaid_code: str) -> str:
        """检测 Mermaid 图表类型"""
        first_line = mermaid_code.split('\n')[0].strip()

        if 'graph' in first_line or 'flowchart' in first_line:
            return 'flowchart'
        elif 'classDiagram' in first_line:
            return 'classDiagram'
        elif 'sequenceDiagram' in first_line:
            return 'sequenceDiagram'
        elif 'stateDiagram' in first_line:
            return 'stateDiagram'
        elif 'xychart' in first_line:
            return 'xychart'
        else:
            return 'flowchart'

    def parse_flowchart(self, code: str) -> Tuple[Dict, List]:
        """解析流程图 Mermaid 代码"""
        nodes = {}
        edges = []

        # 去掉方向声明
        lines = [l.strip() for l in code.split('\n') if l.strip()]

        for line in lines:
            # 跳过配置和注释
            if any(x in line for x in ['---', 'config:', 'direction', '%%']):
                continue

            # 解析节点定义 A["标签"] 或 A[标签]
            node_match = re.match(r'(\w+)\s*\[([^\]]+)\]', line)
            if node_match:
                node_id = node_match.group(1)
                label = node_match.group(2).strip('"')
                nodes[node_id] = label
                continue

            # 解析边 A --> B 或 A -.-> B
            edge_pattern = r'(\w+)\s+(-+>|\.+-+>)\s+(\w+)'
            edge_matches = re.findall(edge_pattern, line)
            for src, arrow, dst in edge_matches:
                style = 'dotted' if '.' in arrow else 'solid'
                edges.append((src, dst, style))

        return nodes, edges

    def generate_dot(self, nodes: Dict, edges: List, diagram_type: str = 'flowchart') -> str:
        """生成 DOT 代码"""
        dot_code = []

        # 图表声明
        dot_code.append('digraph G {')

        # 全局属性 - 优化空间利用率和正交线条
        dot_code.append('    rankdir=LR;')  # 左到右布局（更紧凑）
        dot_code.append('    splines=orthogonal;')  # 正交线条（强制 90° 转向）
        dot_code.append('    nodesep=0.8;')  # 节点间距（紧凑）
        dot_code.append('    ranksep=1.0;')  # 层级间距（紧凑）
        dot_code.append('    layout="dot";')
        dot_code.append('')

        # 节点样式
        dot_code.append('    node [shape=box, style="rounded,filled", fontname="微软雅黑", fontsize=10];')
        dot_code.append('')

        # 根据类型设置节点颜色
        color_map = self._get_color_map()

        # 添加节点
        for node_id, label in nodes.items():
            # 选择颜色
            color = color_map.get(node_id, '#e3f2fd')
            # 清理标签中的 HTML 标签
            clean_label = re.sub(r'<[^>]+>', '\n', label).replace('\\n', '\n')
            dot_code.append(f'    {node_id} [label="{clean_label}", fillcolor="{color}", fontcolor="#000"];')

        dot_code.append('')

        # 边样式
        dot_code.append('    edge [arrowsize=1.2, penwidth=1.5];')
        dot_code.append('')

        # 添加边
        for src, dst, style in edges:
            if style == 'dotted':
                dot_code.append(f'    {src} -> {dst} [style=dotted];')
            else:
                dot_code.append(f'    {src} -> {dst};')

        dot_code.append('}')

        return '\n'.join(dot_code)

    def _get_color_map(self) -> Dict:
        """根据节点名称获取颜色映射"""
        colors = {
            # 框架层
            'GF': '#e3f2fd',
            'GameFramework': '#e3f2fd',
            'UniTask': '#e3f2fd',
            'DOTween': '#e3f2fd',

            # 战斗系统
            'Combat': '#fff3e0',
            'CombatManager': '#fff3e0',
            'Chess': '#fff3e0',
            'ChessEntity': '#fff3e0',

            # Buff/效果
            'Buff': '#f3e5f5',
            'BuffManager': '#f3e5f5',
            'Skill': '#f3e5f5',
            'SkillExecutor': '#f3e5f5',
            'Hit': '#f3e5f5',
            'HitDetector': '#f3e5f5',

            # 内容系统
            'Card': '#e8f5e9',
            'CardManager': '#e8f5e9',
            'Item': '#e8f5e9',
            'InventoryManager': '#e8f5e9',
            'Explore': '#e8f5e9',
            'ExploreManager': '#e8f5e9',

            # 输入/事件
            'Input': '#fce4ec',
            'InputManager': '#fce4ec',
            'PlayerInputManager': '#fce4ec',
            'Event': '#fce4ec',
            'EventManager': '#fce4ec',

            # UI
            'UI': '#fff9c4',
            'UIForm': '#fff9c4',

            # 数据
            'DataTable': '#e0f2f1',
            'Config': '#e0f2f1',
        }
        return colors

    def convert_file(self, md_file: str) -> Optional[str]:
        """转换单个文件"""
        print(f"[PROCESS] {Path(md_file).name}...", end=" ")

        # 提取 Mermaid 代码
        mermaid_code = self.extract_mermaid_code(md_file)
        if not mermaid_code:
            print("[SKIP] 未找到 Mermaid 代码")
            return None

        # 检测类型
        diagram_type = self.detect_diagram_type(mermaid_code)

        # 根据类型转换
        if diagram_type == 'flowchart':
            nodes, edges = self.parse_flowchart(mermaid_code)
            dot_code = self.generate_dot(nodes, edges, diagram_type)
        else:
            # 其他类型暂时保留原样
            print(f"[TODO] 类型 {diagram_type} 需要手动处理")
            return None

        # 生成输出文件
        output_file = md_file.replace('.md', '.dot')
        try:
            with open(output_file, 'w', encoding='utf-8') as f:
                f.write(dot_code)

            file_size = os.path.getsize(output_file) / 1024
            print(f"[OK] → {Path(output_file).name} ({file_size:.1f}KB)")
            return output_file
        except Exception as e:
            print(f"[ERROR] {e}")
            return None


def main():
    """主函数"""
    import sys

    # 工作目录
    work_dir = "d:\\unity\\UnityProject\\GP\\Clash_Of_Gods\\项目知识库（AI）\\图表资源"
    os.chdir(work_dir)

    # 获取所有 .md 文件
    md_files = sorted([f for f in os.listdir('.') if f.endswith('.md')
                      and not any(x in f for x in ['INDEX', '使用指南', '渲染报告', '论文用', '正交'])])

    print(f"[START] 开始转换 {len(md_files)} 个文件")
    print(f"[OUTPUT] 生成格式: Graphviz DOT (适用于 edotor.net)")
    print(f"[OPTIMIZE] 参数: 正交线条 + 空间最优化")
    print()

    converter = MermaidToDotConverter()
    converted = 0
    skipped = 0

    for md_file in md_files:
        result = converter.convert_file(md_file)
        if result:
            converted += 1
        else:
            skipped += 1

    print()
    print(f"[SUMMARY] 完成: {converted} 个成功，{skipped} 个跳过")
    print(f"[OUTPUT] 文件夹: {work_dir}")


if __name__ == "__main__":
    main()
