#!/usr/bin/env node

/**
 * 论文 .docx 生成脚本
 * 使用 docx 库将 Markdown 章节转换为 Word 文档
 * 
 * 使用方法: node generate_thesis_docx.js
 * 
 * 依赖: npm install -g docx
 */

const { Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell, PageBreak,
        AlignmentType, HeadingLevel, BorderStyle, WidthType, ShadingType, VerticalAlign,
        PageNumber, UnderlineType, convertInchesToTwip } = require('docx');
const fs = require('fs');
const path = require('path');

// 配置
const CONFIG = {
  title: 'Clash of Gods 游戏系统设计与实现',
  author: '学生姓名',
  date: new Date().toISOString().split('T')[0],
  
  // 页面设置 (A4)
  pageWidth: 11906,   // A4 宽度 (DXA)
  pageHeight: 16838,  // A4 高度 (DXA)
  marginTop: 2880,    // 2.54cm
  marginBottom: 2880,
  marginLeft: 3175,   // 2.54cm
  marginRight: 3175,
  
  // 字体
  fontBody: '宋体',
  fontHeading: '黑体',
  fontSizeBody: 24,   // 12pt (DXA: 1pt = 2)
  fontSizeH1: 36,     // 18pt
  fontSizeH2: 28,     // 14pt
  fontSizeH3: 24,     // 12pt
  
  // 行距
  lineSpacing: 360,   // 1.5倍 (240 = 1倍)
};

// 章节文件列表
const CHAPTERS = [
  '摘要_ABSTRACT.md',
  '第1章_绪论.md',
  '第2章_相关工作与技术基础.md',
  '第3章_系统总体设计与架构.md',
  '第4章_战斗系统.md',
  '第5章_物品系统.md',
  '第6章_卡牌系统.md',
  '第7章_TA系统.md',
  '第8章_性能优化.md',
  '第9章_总结展望.md',
];

/**
 * 解析 Markdown 内容为段落数组
 */
function parseMarkdown(content) {
  const paragraphs = [];
  const lines = content.split('\n');
  let inCodeBlock = false;
  let codeBlockContent = '';
  let codeBlockLanguage = '';
  
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    
    // 代码块处理
    if (line.startsWith('```')) {
      if (!inCodeBlock) {
        inCodeBlock = true;
        codeBlockLanguage = line.substring(3).trim();
        codeBlockContent = '';
      } else {
        inCodeBlock = false;
        // 添加代码块
        if (codeBlockContent.trim()) {
          paragraphs.push(
            new Paragraph({
              text: codeBlockContent.trim(),
              style: 'Normal',
              spacing: { line: CONFIG.lineSpacing, lineRule: 'auto' },
              border: {
                top: { color: 'CCCCCC', space: 1, style: BorderStyle.SINGLE, size: 6 },
                bottom: { color: 'CCCCCC', space: 1, style: BorderStyle.SINGLE, size: 6 },
                left: { color: 'CCCCCC', space: 1, style: BorderStyle.SINGLE, size: 6 },
                right: { color: 'CCCCCC', space: 1, style: BorderStyle.SINGLE, size: 6 },
              },
              shading: { fill: 'F5F5F5', type: ShadingType.CLEAR },
              indent: { left: 720, right: 720 },
            })
          );
        }
      }
      continue;
    }
    
    if (inCodeBlock) {
      codeBlockContent += line + '\n';
      continue;
    }
    
    // 标题处理
    if (line.startsWith('# ')) {
      paragraphs.push(
        new Paragraph({
          text: line.substring(2).trim(),
          heading: HeadingLevel.HEADING_1,
          spacing: { line: CONFIG.lineSpacing, lineRule: 'auto', before: 240, after: 240 },
        })
      );
      continue;
    }
    
    if (line.startsWith('## ')) {
      paragraphs.push(
        new Paragraph({
          text: line.substring(3).trim(),
          heading: HeadingLevel.HEADING_2,
          spacing: { line: CONFIG.lineSpacing, lineRule: 'auto', before: 180, after: 180 },
        })
      );
      continue;
    }
    
    if (line.startsWith('### ')) {
      paragraphs.push(
        new Paragraph({
          text: line.substring(4).trim(),
          heading: HeadingLevel.HEADING_3,
          spacing: { line: CONFIG.lineSpacing, lineRule: 'auto', before: 120, after: 120 },
        })
      );
      continue;
    }
    
    // 空行处理
    if (line.trim() === '') {
      if (paragraphs.length > 0 && paragraphs[paragraphs.length - 1].text !== '') {
        paragraphs.push(
          new Paragraph({
            text: '',
            spacing: { line: CONFIG.lineSpacing, lineRule: 'auto' },
          })
        );
      }
      continue;
    }
    
    // 普通段落
    if (line.trim()) {
      paragraphs.push(
        new Paragraph({
          text: line.trim(),
          spacing: { line: CONFIG.lineSpacing, lineRule: 'auto' },
        })
      );
    }
  }
  
  return paragraphs;
}

/**
 * 读取章节文件
 */
function readChapter(filename) {
  const filepath = path.join(__dirname, '章节文件', filename);
  try {
    return fs.readFileSync(filepath, 'utf-8');
  } catch (error) {
    console.error(`❌ 无法读取文件: ${filepath}`);
    return '';
  }
}

/**
 * 生成最终文档
 */
function generateThesis() {
  console.log('📝 开始生成论文 .docx 文件...\n');
  
  const allParagraphs = [];
  
  // 添加标题页
  console.log('📄 添加标题页...');
  allParagraphs.push(
    new Paragraph({
      text: CONFIG.title,
      heading: HeadingLevel.HEADING_1,
      alignment: AlignmentType.CENTER,
      spacing: { line: CONFIG.lineSpacing, lineRule: 'auto', before: 240, after: 240 },
    }),
    new Paragraph({
      text: `作者: ${CONFIG.author}`,
      alignment: AlignmentType.CENTER,
      spacing: { line: CONFIG.lineSpacing, lineRule: 'auto' },
    }),
    new Paragraph({
      text: `日期: ${CONFIG.date}`,
      alignment: AlignmentType.CENTER,
      spacing: { line: CONFIG.lineSpacing, lineRule: 'auto', after: 240 },
    }),
    new Paragraph({ text: '', spacing: { line: CONFIG.lineSpacing, lineRule: 'auto' } }),
    new PageBreak()
  );
  
  // 添加各章节
  for (const chapter of CHAPTERS) {
    console.log(`📖 处理章节: ${chapter}`);
    const content = readChapter(chapter);
    
    if (content) {
      const paragraphs = parseMarkdown(content);
      allParagraphs.push(...paragraphs);
      allParagraphs.push(new PageBreak());
    }
  }
  
  // 创建文档
  console.log('\n🔧 创建 Word 文档...');
  const doc = new Document({
    sections: [{
      properties: {
        page: {
          margin: {
            top: CONFIG.marginTop,
            bottom: CONFIG.marginBottom,
            left: CONFIG.marginLeft,
            right: CONFIG.marginRight,
          },
        },
      },
      footers: {
        default: new (require('docx').Footer)({
          children: [
            new Paragraph({
              children: [
                new TextRun('第 '),
                new TextRun({ children: [PageNumber.CURRENT] }),
                new TextRun(' 页'),
              ],
              alignment: AlignmentType.CENTER,
            }),
          ],
        }),
      },
      children: allParagraphs,
    }],
    styles: {
      default: {
        document: {
          run: {
            font: CONFIG.fontBody,
            size: CONFIG.fontSizeBody,
          },
          paragraph: {
            spacing: { line: CONFIG.lineSpacing, lineRule: 'auto' },
          },
        },
      },
      paragraphStyles: [
        {
          id: 'Heading1',
          name: 'Heading 1',
          basedOn: 'Normal',
          next: 'Normal',
          run: {
            font: CONFIG.fontHeading,
            size: CONFIG.fontSizeH1,
            bold: true,
          },
          paragraph: {
            spacing: { line: CONFIG.lineSpacing, lineRule: 'auto', before: 240, after: 240 },
            outlineLevel: 0,
          },
        },
        {
          id: 'Heading2',
          name: 'Heading 2',
          basedOn: 'Normal',
          next: 'Normal',
          run: {
            font: CONFIG.fontHeading,
            size: CONFIG.fontSizeH2,
            bold: true,
          },
          paragraph: {
            spacing: { line: CONFIG.lineSpacing, lineRule: 'auto', before: 180, after: 180 },
            outlineLevel: 1,
          },
        },
        {
          id: 'Heading3',
          name: 'Heading 3',
          basedOn: 'Normal',
          next: 'Normal',
          run: {
            font: CONFIG.fontHeading,
            size: CONFIG.fontSizeH3,
            bold: true,
          },
          paragraph: {
            spacing: { line: CONFIG.lineSpacing, lineRule: 'auto', before: 120, after: 120 },
            outlineLevel: 2,
          },
        },
      ],
    },
  });
  
  // 保存文档
  const outputPath = path.join(__dirname, 'Clash_Of_Gods_毕业论文_最终版.docx');
  console.log(`\n💾 保存文档到: ${outputPath}`);
  
  Packer.toBuffer(doc).then(buffer => {
    fs.writeFileSync(outputPath, buffer);
    console.log(`\n✅ 论文生成成功！`);
    console.log(`📊 文档信息:`);
    console.log(`   - 标题: ${CONFIG.title}`);
    console.log(`   - 作者: ${CONFIG.author}`);
    console.log(`   - 章节数: ${CHAPTERS.length}`);
    console.log(`   - 文件大小: ${(buffer.length / 1024).toFixed(2)} KB`);
    console.log(`   - 输出路径: ${outputPath}`);
  }).catch(error => {
    console.error(`\n❌ 生成失败: ${error.message}`);
    process.exit(1);
  });
}

// 执行
generateThesis();
