#!/usr/bin/env node
/**
 * 生成毕业论文 .docx 文件
 * 使用 docx 库合并所有章节并应用格式
 */

const { Document, Packer, Paragraph, TextRun, PageBreak, HeadingLeve