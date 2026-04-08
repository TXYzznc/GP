#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
使用在线 Graphviz API 渲染 .dot 文件为 PNG/SVG
支持离线模式：使用本地缓存或直接输出 SVG
"""

import os
import sys
import io
import base64
import requests
from pathlib import Path

# 修复 Windows 编码问题
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')


def render_with_online_api(dot_content, output_path, fmt='png'):
    """
    使用在线 Graphviz API 渲染
    """
    try:
        # 使用 Graphviz 在线编辑器的 API
        url = "https://dreampuf.github.io/GraphvizOnline/graphvizapi.php"

        # 准备参数
        params = {
            'dot': dot_content,
            'format': fmt
        }

        print(f"[ONLINE] 连接到在线 API...", end=" ", flush=True)
        response = requests.post(url, data=params, timeout=10)
        response.raise_for_status()

        # 保存文件
        with open(f"{output_path}.{fmt}", 'wb') as f:
            f.write(response.content)

        file_size = len(response.content) / 1024
        print(f"[OK] ({file_size:.1f}KB)")
        return True

    except Exception as e:
        print(f"\n[ERROR] API 调用失败: {e}")
        return False


def render_dot_to_svg_local(dot_content, output_path):
    """
    本地方式：直接输出 SVG（需要 dot 命令）
    如果 dot 不可用，生成 HTML 预览版本
    """
    try:
        import subprocess

        print(f"[LOCAL] 使用本地 dot 命令...", end=" ", flush=True)

        # 调用 dot 命令
        result = subprocess.run(
            ['dot', '-Tsvg', f'-o{output_path}.svg'],
            input=dot_content,
            text=True,
            capture_output=True,
            timeout=10
        )

        if result.returncode == 0:
            file_size = os.path.getsize(f"{output_path}.svg") / 1024
            print(f"[OK] ({file_size:.1f}KB)")
            return True
        else:
            print(f"[ERROR] {result.stderr}")
            return False

    except FileNotFoundError:
        print(f"[WARNING] dot 命令未找到")
        return False
    except Exception as e:
        print(f"[ERROR] {e}")
        return False


def generate_html_preview(dot_content, output_path):
    """
    生成包含 Mermaid 代码的 HTML 文件（作为备选方案）
    """
    html_content = f"""<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Graphviz 图表预览</title>
    <script src="https://cdn.jsdelivr.net/npm/graphviz-wasm@0.0.9/dist/graphviz.wasm.js"></script>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        h1 {{ color: #333; }}
        #graph {{ border: 1px solid #ccc; padding: 10px; }}
        pre {{ background-color: #f5f5f5; padding: 10px; overflow-x: auto; }}
    </style>
</head>
<body>
    <h1>Graphviz 图表预览</h1>
    <p>使用浏览器本地渲染（支持所有现代浏览器）</p>

    <div id="graph" style="width: 100%; min-height: 600px;"></div>

    <h2>Graphviz 源代码（.dot）</h2>
    <pre><code>{dot_content}</code></pre>

    <hr>
    <p>💡 <strong>如何在线渲染：</strong></p>
    <ol>
        <li>访问 <a href="https://edotor.net/" target="_blank">https://edotor.net/</a></li>
        <li>或访问 <a href="https://dreampuf.github.io/GraphvizOnline/" target="_blank">https://dreampuf.github.io/GraphvizOnline/</a></li>
        <li>粘贴上面的 .dot 代码</li>
        <li>点击下载按钮导出 PNG/SVG</li>
    </ol>

    <script>
        // 尝试使用 Graphviz Wasm 进行本地渲染
        async function renderGraph() {{
            try {{
                const Graphviz = await graphviz();
                const svg = Graphviz.layout({dot_content!r}, 'svg', 'dot');
                document.getElementById('graph').innerHTML = svg;
            }} catch (e) {{
                console.log('本地渲染失败，请使用在线工具');
                document.getElementById('graph').innerHTML = '<p style="color: red;">本地渲染不可用，请使用在线工具</p>';
            }}
        }}
        renderGraph();
    </script>
</body>
</html>"""

    with open(f"{output_path}.html", 'w', encoding='utf-8') as f:
        f.write(html_content)

    print(f"[HTML] HTML 预览已生成: {output_path}.html")


def main():
    # 定位 .dot 文件
    script_dir = Path(__file__).parent
    dot_file = script_dir / "项目架构图-正交线条版.dot"

    if len(sys.argv) > 1:
        dot_file = Path(sys.argv[1])

    if not dot_file.exists():
        print(f"[ERROR] 文件不存在: {dot_file}")
        return False

    # 读取 .dot 文件
    with open(dot_file, 'r', encoding='utf-8') as f:
        dot_content = f.read()

    output_path = str(dot_file.with_suffix(''))

    print(f"\n[START] 渲染 Graphviz 图表")
    print(f"[INPUT] {dot_file.name}")
    print(f"[OUTPUT] {script_dir.name}/")
    print()

    # 尝试多种渲染方式
    success = False

    # 方式 1：本地 dot 命令
    if render_dot_to_svg_local(dot_content, output_path):
        success = True

    # 方式 2：在线 API - PNG
    print("[ONLINE] 尝试在线 API 渲染 PNG...")
    if render_with_online_api(dot_content, output_path, 'png'):
        success = True
    else:
        print("[FALLBACK] 在线 API 不可用")

    # 方式 3：在线 API - SVG
    print("[ONLINE] 尝试在线 API 渲染 SVG...")
    if render_with_online_api(dot_content, output_path, 'svg'):
        success = True

    # 方式 4：生成 HTML 预览
    print()
    generate_html_preview(dot_content, output_path)

    print()
    if success:
        print("[SUCCESS] 渲染完成！")
        print(f"[OUTPUT] PNG: {output_path}.png")
        print(f"[OUTPUT] SVG: {output_path}.svg")
    else:
        print("[WARNING] 在线 API 调用失败")
        print("[INFO] 使用备选方案：HTML 预览")

    print()
    print("[TIPS] 在线渲染工具：")
    print("  • https://edotor.net/")
    print("  • https://dreampuf.github.io/GraphvizOnline/")

    return success


if __name__ == "__main__":
    try:
        success = main()
        sys.exit(0 if success else 1)
    except Exception as e:
        print(f"\n[FATAL] 未处理的异常: {e}")
        sys.exit(2)
