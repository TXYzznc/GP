# Tasks: Knowledge Base Refactoring

## 1. Template Design & Validation

- [ ] 1.1 Design System Design Document template
- [ ] 1.2 Design Technical Documentation template
- [ ] 1.3 Design Development Summary template
- [ ] 1.4 Design Development Notes template
- [ ] 1.5 Design Problem Diagnosis template
- [ ] 1.6 Design Project Rules template
- [ ] 1.7 Test templates on 3-5 existing documents
- [ ] 1.8 Collect feedback and finalize templates
- [ ] 1.9 Create template examples with instructions
- [ ] 1.10 Document metadata header standard (date, status, tags)

## 2. Project Rules Refactoring (High Priority)

- [ ] 2.1 Refactor 代码规范.md according to template
- [ ] 2.2 Refactor 框架使用规则.md according to template
- [ ] 2.3 Refactor 配置表使用与设计规则.md according to template
- [ ] 2.4 Refactor 资源加载规则.md according to template
- [ ] 2.5 Refactor 工具使用规则.md according to template
- [ ] 2.6 Verify all cross-references in project rules
- [ ] 2.7 Update project rules README.md

## 3. Technical Documentation Refactoring (High Priority)

- [ ] 3.1 Inventory all 45+ technical documents (list filenames)
- [ ] 3.2 Identify and mark duplicate technical documentation
- [ ] 3.3 Consolidate duplicate API/configuration documentation
- [ ] 3.4 Refactor 技术文档 folder: 使用说明 subsection (10+ docs)
- [ ] 3.5 Refactor 技术文档 folder: 配置指南 subsection (10+ docs)
- [ ] 3.6 Refactor 技术文档 folder: API参考 subsection (15+ docs)
- [ ] 3.7 Validate all code examples in technical docs
- [ ] 3.8 Validate all configuration table references
- [ ] 3.9 Update technical documentation index

## 4. System Design Documentation Refactoring (Medium Priority)

- [ ] 4.1 Inventory all 40+ system design documents
- [ ] 4.2 Identify related design documents that should be consolidated
- [ ] 4.3 Organize design docs by system (combat, exploration, chess, UI, etc.)
- [ ] 4.4 Refactor combat system design documents (8+ docs)
- [ ] 4.5 Refactor exploration system design documents (5+ docs)
- [ ] 4.6 Refactor chess/summon system design documents (5+ docs)
- [ ] 4.7 Refactor UI system design documents (5+ docs)
- [ ] 4.8 Refactor configuration table design documents (5+ docs)
- [ ] 4.9 Refactor other system design documents (7+ docs)
- [ ] 4.10 Verify all cross-references between design documents
- [ ] 4.11 Update system design documentation index

## 5. Development Summary Refactoring (Medium Priority)

- [ ] 5.1 Inventory all 15+ development summary documents
- [ ] 5.2 Mark outdated summaries (older than 3 months)
- [ ] 5.3 Refactor recent development summaries (Phase 11-16 related)
- [ ] 5.4 Archive outdated development summaries
- [ ] 5.5 Organize summaries by phase and timeline
- [ ] 5.6 Remove redundant content from summaries
- [ ] 5.7 Verify all file references in summaries still exist
- [ ] 5.8 Update development summary index

## 6. Development Notes & Problem Diagnosis Refactoring (Lower Priority)

- [ ] 6.1 Inventory all development notes (10+ docs)
- [ ] 6.2 Identify outdated best practices and mark them
- [ ] 6.3 Refactor development notes according to template
- [ ] 6.4 Consolidate duplicate best practices
- [ ] 6.5 Inventory all problem diagnosis documents (15+ docs)
- [ ] 6.6 Mark resolved/obsolete diagnoses
- [ ] 6.7 Refactor problem diagnosis documents according to template
- [ ] 6.8 Archive outdated problem diagnoses
- [ ] 6.9 Update notes & diagnosis index

## 7. Context Cache & Phase-Specific Content (Lower Priority)

- [ ] 7.1 Review all context cache documents
- [ ] 7.2 Archive outdated context caches (keep last 5)
- [ ] 7.3 Organize "战斗与探索系统优化" folder by phase
- [ ] 7.4 Mark Phase 11-13 content as historical
- [ ] 7.5 Create cross-references from phase content to current systems

## 8. Knowledge Base Index & Navigation

- [ ] 8.1 Update INDEX.md to reflect consolidated structure
- [ ] 8.2 Remove duplicate entries from INDEX.md
- [ ] 8.3 Add priority/frequency tags to commonly-used docs
- [ ] 8.4 Create "Getting Started" section in README.md
- [ ] 8.5 Create quick reference links for most-used documents
- [ ] 8.6 Update file organization structure in README.md
- [ ] 8.7 Add document count summary by type

## 9. Validation & Quality Assurance

- [ ] 9.1 Run content accuracy review on all refactored documents
- [ ] 9.2 Validate all internal document links (using grep or automated tool)
- [ ] 9.3 Validate all code file references (ensure files still exist)
- [ ] 9.4 Validate all configuration table references (match actual tables)
- [ ] 9.5 Check terminology consistency across all documents
- [ ] 9.6 Verify all documents have proper metadata headers
- [ ] 9.7 Test all code examples compile/run correctly

## 10. Final Review & Documentation

- [ ] 10.1 Create document maintenance guidelines
- [ ] 10.2 Create template update process documentation
- [ ] 10.3 Create new document checklist for future authors
- [ ] 10.4 Archive old documentation and create changelog
- [ ] 10.5 Create documentation style guide summary
- [ ] 10.6 Final full knowledge base review
- [ ] 10.7 Update last modified date on all files
- [ ] 10.8 Create completion summary document

## Notes

**Estimated Work**: 20-30 days total (with focus on high-priority items first)

**Parallel Work Opportunity**:
- Design & specs phases can be reviewed in parallel with implementation
- Multiple document types can be refactored simultaneously by different team members

**Quality Checkpoints**:
- After Phase 1: Verify templates are effective on sample docs
- After Phase 2: Verify high-priority docs are correct before proceeding
- After Phase 8: Ensure navigation is working before final review
- After Phase 9: All validation checks pass

**Risk Mitigation**:
- Keep backups of original documents before refactoring
- Use git to track all changes
- Document rationale for significant content changes in commit messages
