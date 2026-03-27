#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
TXT to XLSX Converter GUI
带图形界面的配置表转换工具，支持拖拽和文件选择
"""

import sys
import os
import subprocess
from pathlib import Path
from typing import Optional

# 检查并安装依赖
def check_and_install_dependencies():
    """检查并自动安装缺失的依赖"""
    try:
        from PyQt6.QtWidgets import QApplication
        return True
    except ImportError:
        print("[信息] 检测到缺少 PyQt6 库，正在自动安装...")
        try:
            subprocess.check_call([sys.executable, "-m", "pip", "install", "PyQt6", "-i", "https://pypi.org/simple/"])
            print("[成功] PyQt6 安装完成")
            return True
        except subprocess.CalledProcessError:
            print("[错误] PyQt6 安装失败")
            print("请手动运行: python -m pip install PyQt6")
            return False

if not check_and_install_dependencies():
    sys.exit(1)

try:
    from PyQt6.QtWidgets import (
        QApplication, QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
        QPushButton, QLabel, QLineEdit, QTextEdit, QFileDialog,
        QProgressBar, QMessageBox, QFrame
    )
    from PyQt6.QtCore import Qt, QMimeData, pyqtSignal, QObject, QThread, QSize
    from PyQt6.QtGui import QFont, QColor, QDragEnterEvent, QDropEvent
except ImportError as e:
    print(f"[错误] 导入 PyQt6 失败: {e}")
    sys.exit(1)

# 导入转换器
try:
    from txt_to_xlsx_converter import TxtToXlsxConverter
except ImportError as e:
    print(f"[错误] 导入转换器失败: {e}")
    print("[信息] 请确保 txt_to_xlsx_converter.py 在同一目录")
    sys.exit(1)


class DragDropLineEdit(QLineEdit):
    """支持拖拽的输入框"""
    file_dropped = pyqtSignal(str)
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setAcceptDrops(True)
    
    def dragEnterEvent(self, event: QDragEnterEvent):
        if event.mimeData().hasUrls():
            event.acceptProposedAction()
    
    def dropEvent(self, event: QDropEvent):
        urls = event.mimeData().urls()
        if urls:
            path = urls[0].toLocalFile()
            self.setText(path)
            self.file_dropped.emit(path)


class TxtToXlsxConverterGUI(QMainWindow):
    """TXT to XLSX 转换器 GUI"""
    
    def __init__(self):
        super().__init__()
        self.converter = TxtToXlsxConverter(auto_open_folder=False)
        self.init_ui()
    
    def init_ui(self):
        """初始化界面"""
        self.setWindowTitle("TXT to XLSX 转换工具")
        self.setGeometry(100, 100, 700, 600)
        
        # 设置字体
        font = QFont()
        font.setPointSize(10)
        self.setFont(font)
        
        # 主窗口
        central_widget = QWidget()
        self.setCentralWidget(central_widget)
        main_layout = QVBoxLayout(central_widget)
        main_layout.setSpacing(15)
        main_layout.setContentsMargins(20, 20, 20, 20)
        
        # ===== 输入文件/文件夹 =====
        input_label = QLabel("输入文件或文件夹:")
        input_label.setFont(self._bold_font())
        main_layout.addWidget(input_label)
        
        input_layout = QHBoxLayout()
        self.input_edit = DragDropLineEdit()
        self.input_edit.setPlaceholderText("拖拽文件/文件夹到此，或点击按钮选择...")
        self.input_edit.file_dropped.connect(self.on_input_dropped)
        input_layout.addWidget(self.input_edit)
        
        input_btn = QPushButton("选择")
        input_btn.setMaximumWidth(80)
        input_btn.clicked.connect(self.select_input)
        input_layout.addWidget(input_btn)
        
        main_layout.addLayout(input_layout)
        
        # ===== 输出文件夹 =====
        output_label = QLabel("输出文件夹:")
        output_label.setFont(self._bold_font())
        main_layout.addWidget(output_label)
        
        output_layout = QHBoxLayout()
        self.output_edit = DragDropLineEdit()
        self.output_edit.setPlaceholderText("拖拽文件夹到此，或点击按钮选择...")
        self.output_edit.file_dropped.connect(self.on_output_dropped)
        output_layout.addWidget(self.output_edit)
        
        output_btn = QPushButton("选择")
        output_btn.setMaximumWidth(80)
        output_btn.clicked.connect(self.select_output)
        output_layout.addWidget(output_btn)
        
        main_layout.addLayout(output_layout)
        
        # ===== 转换按钮 =====
        button_layout = QHBoxLayout()
        button_layout.addStretch()
        
        self.convert_btn = QPushButton("开始转换")
        self.convert_btn.setMinimumHeight(40)
        self.convert_btn.setMinimumWidth(150)
        self.convert_btn.setStyleSheet("""
            QPushButton {
                background-color: #4CAF50;
                color: white;
                font-weight: bold;
                border-radius: 5px;
                padding: 10px;
            }
            QPushButton:hover {
                background-color: #45a049;
            }
            QPushButton:pressed {
                background-color: #3d8b40;
            }
            QPushButton:disabled {
                background-color: #cccccc;
            }
        """)
        self.convert_btn.clicked.connect(self.start_conversion)
        button_layout.addWidget(self.convert_btn)
        
        button_layout.addStretch()
        main_layout.addLayout(button_layout)
        
        # ===== 进度条 =====
        self.progress_bar = QProgressBar()
        self.progress_bar.setVisible(False)
        self.progress_bar.setMaximum(0)  # 不确定进度
        main_layout.addWidget(self.progress_bar)
        
        # ===== 日志输出 =====
        log_label = QLabel("转换日志:")
        log_label.setFont(self._bold_font())
        main_layout.addWidget(log_label)
        
        self.log_text = QTextEdit()
        self.log_text.setReadOnly(True)
        self.log_text.setMinimumHeight(200)
        self.log_text.setStyleSheet("""
            QTextEdit {
                background-color: #f5f5f5;
                border: 1px solid #ddd;
                border-radius: 3px;
                padding: 5px;
                font-family: 'Courier New', monospace;
                font-size: 9pt;
            }
        """)
        main_layout.addWidget(self.log_text)
        
        # ===== 底部按钮 =====
        footer_layout = QHBoxLayout()
        
        clear_btn = QPushButton("清空日志")
        clear_btn.setMaximumWidth(100)
        clear_btn.clicked.connect(self.log_text.clear)
        footer_layout.addWidget(clear_btn)
        
        footer_layout.addStretch()
        main_layout.addLayout(footer_layout)
        
        # 初始化日志
        self.log("欢迎使用 TXT to XLSX 转换工具！")
        self.log("=" * 50)
        self.log("使用方法:")
        self.log("1. 拖拽或选择输入文件/文件夹")
        self.log("2. 拖拽或选择输出文件夹")
        self.log("3. 点击 '开始转换' 按钮")
        self.log("=" * 50)
    
    def _bold_font(self) -> QFont:
        """获取加粗字体"""
        font = QFont()
        font.setBold(True)
        return font
    
    def log(self, message: str):
        """添加日志"""
        self.log_text.append(message)
        # 自动滚动到底部
        self.log_text.verticalScrollBar().setValue(
            self.log_text.verticalScrollBar().maximum()
        )
    
    def select_input(self):
        """选择输入文件或文件夹"""
        dialog = QFileDialog(self)
        dialog.setWindowTitle("选择输入文件或文件夹")
        dialog.setFileMode(QFileDialog.FileMode.AnyFile)
        
        if dialog.exec() == QFileDialog.DialogCode.Accepted:
            path = dialog.selectedFiles()[0]
            self.input_edit.setText(path)
            self.log(f"[成功] 已选择输入: {path}")
    
    def select_output(self):
        """选择输出文件夹"""
        folder = QFileDialog.getExistingDirectory(
            self,
            "选择输出文件夹",
            os.path.expanduser("~")
        )
        if folder:
            self.output_edit.setText(folder)
            self.log(f"[成功] 已选择输出: {folder}")
    
    def on_input_dropped(self, path: str):
        """处理输入拖拽"""
        self.log(f"[成功] 已拖拽输入: {path}")
    
    def on_output_dropped(self, path: str):
        """处理输出拖拽"""
        if os.path.isdir(path):
            self.log(f"[成功] 已拖拽输出: {path}")
        else:
            # 如果拖拽的是文件，使用其所在目录
            folder = os.path.dirname(path)
            self.output_edit.setText(folder)
            self.log(f"[成功] 已拖拽输出 (使用文件所在目录): {folder}")
    
    def start_conversion(self):
        """开始转换"""
        input_path = self.input_edit.text().strip()
        output_dir = self.output_edit.text().strip()
        
        # 验证输入
        if not input_path:
            QMessageBox.warning(self, "警告", "请选择输入文件或文件夹")
            return
        
        if not os.path.exists(input_path):
            QMessageBox.warning(self, "警告", f"输入路径不存在: {input_path}")
            return
        
        if not output_dir:
            QMessageBox.warning(self, "警告", "请选择输出文件夹")
            return
        
        # 创建输出目录
        try:
            os.makedirs(output_dir, exist_ok=True)
        except Exception as e:
            QMessageBox.warning(self, "警告", f"无法创建输出目录: {str(e)}")
            return
        
        # 禁用按钮
        self.convert_btn.setEnabled(False)
        self.progress_bar.setVisible(True)
        
        # 清空日志
        self.log_text.clear()
        self.log("=" * 50)
        self.log("开始转换...")
        self.log("=" * 50)
        
        # 执行转换（同步，不使用线程）
        self.perform_conversion(input_path, output_dir)
    
    def perform_conversion(self, input_path: str, output_dir: str):
        """执行转换"""
        output_files = []
        
        try:
            if os.path.isfile(input_path):
                # 单文件转换
                self.log(f"处理文件: {os.path.basename(input_path)}")
                try:
                    success = self.converter.convert_file_with_processor(
                        input_path, 
                        output_dir
                    )
                    
                    if success:
                        base_name = os.path.splitext(os.path.basename(input_path))[0]
                        output_file = os.path.join(output_dir, f"{base_name}.xlsx")
                        if os.path.exists(output_file):
                            output_files.append(output_file)
                        self.on_conversion_success(output_files)
                    else:
                        self.on_conversion_failed("[错误] 转换失败，请检查输入文件格式")
                except Exception as e:
                    self.log(f"[错误] 转换异常: {str(e)}")
                    import traceback
                    self.log(traceback.format_exc())
                    self.on_conversion_failed(f"[错误] 转换异常: {str(e)}")
            
            elif os.path.isdir(input_path):
                # 目录批量转换
                txt_files = [f for f in os.listdir(input_path) if f.endswith('.txt')]
                self.log(f"找到 {len(txt_files)} 个 TXT 文件")
                
                if not txt_files:
                    self.on_conversion_failed("[错误] 目录中没有找到 TXT 文件")
                    return
                
                success_count = 0
                for i, txt_file in enumerate(txt_files, 1):
                    self.log(f"[{i}/{len(txt_files)}] 转换 {txt_file}...")
                    txt_path = os.path.join(input_path, txt_file)
                    try:
                        if self.converter.convert_file_with_processor(txt_path, output_dir):
                            success_count += 1
                            base_name = os.path.splitext(txt_file)[0]
                            output_file = os.path.join(output_dir, f"{base_name}.xlsx")
                            if os.path.exists(output_file):
                                output_files.append(output_file)
                    except Exception as e:
                        self.log(f"[警告] {txt_file} 转换失败: {str(e)}")
                
                msg = f"[成功] 批量转换完成！\n成功: {success_count}/{len(txt_files)}"
                self.log(msg)
                
                if success_count > 0:
                    self.on_conversion_success(output_files)
                else:
                    self.on_conversion_failed("[错误] 所有文件转换失败")
            else:
                self.on_conversion_failed("[错误] 输入路径无效")
        
        except Exception as e:
            import traceback
            self.log(f"[错误] 未预期的异常: {str(e)}")
            self.log(traceback.format_exc())
            self.on_conversion_failed(f"[错误] 转换出错: {str(e)}")
    
    def on_conversion_success(self, output_files: list):
        """转换成功"""
        self.log("=" * 50)
        self.log("[成功] 转换完成！")
        self.log("=" * 50)
        
        # 启用按钮
        self.convert_btn.setEnabled(True)
        self.progress_bar.setVisible(False)
        
        # 显示消息框
        QMessageBox.information(self, "成功", "转换完成！")
        
        # 打开输出文件夹并选中文件
        if output_files:
            self.open_output_folder_with_files(output_files)
    
    def on_conversion_failed(self, message: str):
        """转换失败"""
        self.log("=" * 50)
        self.log(message)
        self.log("=" * 50)
        
        # 启用按钮
        self.convert_btn.setEnabled(True)
        self.progress_bar.setVisible(False)
        
        # 显示消息框
        QMessageBox.warning(self, "失败", message)
    
    def open_output_folder_with_files(self, file_paths: list):
        """打开输出文件夹并选中文件"""
        if not file_paths:
            return
        
        import platform
        import time
        
        system = platform.system()
        try:
            if system == 'Windows':
                # Windows: 使用 explorer /select 选中文件
                # 先检查文件夹是否已打开，如果已打开则直接选中文件
                for file_path in file_paths:
                    if os.path.exists(file_path):
                        # 使用 explorer /select 会自动打开文件夹（如果未打开）或在已打开的窗口中选中文件
                        subprocess.Popen(['explorer', '/select,', os.path.abspath(file_path)])
                        break  # 只处理第一个文件
            elif system == 'Darwin':
                # macOS: 使用 open -R 打开并选中
                if os.path.exists(file_paths[0]):
                    subprocess.Popen(['open', '-R', file_paths[0]])
            else:
                # Linux: 使用 xdg-open 打开文件夹
                folder = os.path.dirname(file_paths[0])
                subprocess.Popen(['xdg-open', folder])
            
            self.log("[成功] 已选中输出文件")
        except Exception as e:
            self.log(f"[警告] 无法打开文件夹: {str(e)}")


def main():
    """主函数"""
    print("[main] 程序启动")
    app = QApplication(sys.argv)
    
    # 设置应用样式
    app.setStyle('Fusion')
    
    # 创建主窗口
    print("[main] 创建主窗口")
    window = TxtToXlsxConverterGUI()
    window.show()
    print("[main] 窗口显示")
    
    print("[main] 进入事件循环")
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
