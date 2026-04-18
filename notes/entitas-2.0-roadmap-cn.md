# Entitas 2.0 愿景与稳定化路线图

本文档用于记录 Entitas 2.0-beta 背后较可能的原始愿景、它为何似乎停留在 beta 阶段，以及将其推进到真正 2.0 正式版所需的具体路线图。

目标是为未来会话保留上下文，避免项目重新退化为零散的一次性修补，而缺少明确的发布策略。

## 为什么需要这个文档

上游 Entitas 的开发似乎在 Entitas 2.0 最终完成之前就已经放缓或停止。

最强的公开信号包括：

- 上游 issue `#1005`：`Entitas with Roslyn Code Generation via dotnet IIncrementalGenerator`
- 上游 issue `#1074`：`Update on Entitas 2.0-beta and my situation`
- 上游 discussion `#1008`：`One Entitas version - Desperate Devs is open-source now`
- 上游没有任何比 `1.14.1` 更新的 release

这些都表明：2.0 的真实转型确实启动过，但在发布加固、迁移打磨、打包和文档完成之前被中断了。

## 还原出来的原始愿景

基于上游公开表述和当前代码库，Entitas 2.0 的预期方向大概率是：

1. 用 Roslyn 增量/源码生成器替换 Jenny。
2. 简化 Entitas 代码库并减少项目数量。
3. 更自然地支持 Unity asmdef 和多项目结构。
4. 支持带命名空间的 context 和 component。
5. 在可能范围内移除或减少对 `DesperateDevs` 的依赖。
6. 让 Entitas 能够作为 Unity package 干净地使用。
7. 围绕 SDK-style 项目和现代 .NET 工具链现代化整个仓库。
8. 保持 Unity 作为主要用户体验。

这与 Simon Schmid 在 issue `#1074` 中的公开总结是一致的，他提到：

- 用新的 .NET source generator 替换 Jenny
- 支持 Unity asmdef
- 支持带命名空间的 context 和 component
- 移除对 DesperateDevs 的依赖
- 将 Entitas 作为 Unity package 使用
- 迁移到 .NET 6
- 大幅简化代码库

## 为什么 Entitas 2.0 一直停留在 Beta

公开证据表明，beta 状态并不只是代码质量问题，更主要是“在完成之前被中断了”。

### 可能的非技术原因

issue `#1074` 明确写到：开发因为裁员以及维护者个人处境变化而中断。

这很可能阻止了最后一段冲刺，包括：

- 持续开发
- 在真实项目中的内部使用
- 迁移打磨
- 打包与文档完善
- 正式公开发布

### 可能的技术/产品原因

即使核心方向已经确定，仍有若干发布级关键区域显然未完成：

1. 旧生成器路径和新生成器路径并存。
2. 文档仍主要描述旧的 Jenny 工作流。
3. 示例项目仍使用旧属性和旧初始化模式。
4. 增量生成器仍包含硬编码的程序集名称假设。
5. Unity package 方案尚未完成。
6. 可视化调试/编辑器支持似乎仍绑定于旧假设和旧版 Unity 行为。
7. 对真实用户而言，没有一个打磨完成的 Entitas 1 -> 2 迁移路径。

换句话说：架构转向已经发生了，但产品化阶段没有完整收尾。

## 这个 Fork 当前的状态

当前 fork 已经实质性地朝着预期的 2.0 方向推进了。

### 已完成或已改进的部分

- `gen/Entitas.CodeGeneration` 已集成进 solution。
- `src/Entitas.CodeGeneration.Attributes` 已集成进 solution。
- `tests/Entitas.CodeGeneration.Tests` 中已经有端到端集成覆盖。
- solution 和 Rider 的加载体验已修复，仓库默认可干净构建。
- `README.md` 已重写为 Unity-first 和 incremental-generator-first。
- `AGENTS.md` 已反映当前项目结构和验证流程。

### 仍然明显未完成的部分

- `gen/Entitas.Generators` 仍然存在。
- `src/Entitas.Generators.Attributes` 仍然存在。
- `samples/Unity` 仍然指向旧生成器属性命名空间和旧的 setup 模型。
- `EntitasUpgradeGuide.md` 仍然反映旧生成器时代，而不是当前 2.0 路径。
- `src/Entitas.Unity.Editor` 仍保留对旧属性程序集的遗留引用。
- `gen/Entitas.CodeGeneration/EntitasGenerator.cs` 仍然硬编码支持的程序集名称。
- 还没有最终版的 release checklist 或 2.0 的完成定义。

## 一个真正的 Entitas 2.0 Release 的定义

只有当以下所有条件都满足时，这个项目才应被视为有效的 2.0 正式版。

### 产品层面的期望

1. Unity 开发者无需 Jenny 就能安装和使用 Entitas 2.0。
2. 增量生成器在默认 Unity 路径和自定义 asmdef 配置中都能工作。
3. 主文档准确描述当前工作流。
4. 现有 Entitas 1 用户有迁移路径。
5. 示例项目展示了新工作流。
6. Unity 面向用户的工具在受支持 Unity 版本上稳定可用。
7. 旧生成器路径要么被移除，要么被明确标记为 legacy。

### 工程层面的期望

1. CI 中完整 solution 的 build 和 test 全部通过。
2. 增量生成器输出有针对性的测试覆盖。
3. Unity 集成和编辑器行为被测试到足以信任发布。
4. 配置假设是显式的，而不是硬编码的。
5. 发布产物和版本管理是一致的。

## 需要补齐的主要缺口

### 1. 生成器迁移尚未完成

最大的结构性缺口在于：仓库仍跨在两套代码生成体系之间。

问题：

- 新路径：`Entitas.CodeGeneration`
- 旧路径：`Entitas.Generators`
- 示例和部分 Unity/Editor 代码里仍出现旧 attributes
- 用户仍可能收到相互矛盾的信号，不知道哪条工作流才是 canonical

目标状态：

- 一条明确的主生成器路径
- 一个明确的主 attributes 命名空间
- 旧路径要么移除，要么被隔离并标记为 legacy

### 2. 程序集名称过滤仍是原型期限制

新生成器目前只会在硬编码的程序集名称上运行。

问题：

- 对测试和默认 `Assembly-CSharp` 能工作
- 对使用 asmdef 的真实 Unity 项目很脆弱
- 这削弱了它最初的目标之一：正确支持 asmdef

目标状态：

- 可配置的程序集 include/exclude 规则
- 对普通 Unity 用户有效的默认行为
- 对自定义 asmdef 有文档化行为

### 3. Unity package 方案未完成

上游明确指向 Unity package 用法，但当前仓库还不是一个打磨完成的 package 产品。

问题：

- 没有完成的 UPM 导向 package 结构
- Unity 用户的 analyzer 分发方案还没有最终定型
- 没有面向 Unity package 消费者的端到端安装文档

目标状态：

- 一个有文档且可重复的 Unity package 工作流
- 一个关于 runtime 程序集、editor 程序集和 analyzer 投递方式的清晰方案

### 4. 示例项目尚未对齐新架构

示例项目对用户来说仍是重要的事实标准，但它目前仍保留了旧生成器模式。

问题：

- 仍使用旧的 `Entitas.Generators.Attributes`
- 仍保留旧的 `ContextInitialization` 流程
- 示例代码没有教会用户新的主工作流

目标状态：

- 示例使用 `Entitas.CodeGeneration.Attributes`
- 示例展示 `Contexts` 和新的生成 API
- 示例可作为 2.0 采用时的有效参考

### 5. 文档和升级指引不完整

文档在这个 fork 开始更新之前，就是 beta 阶段的主要阻碍之一。

问题：

- 上游 setup 指引混乱到用户不得不提交临时 setup 问题
- `EntitasUpgradeGuide.md` 对 2.0 迁移已过时
- 没有一份从 Jenny 迁移到 incremental generation 的实用指南

目标状态：

- Unity-first 安装文档
- Entitas 1 -> 2 迁移指南
- 对 Rider、Unity、asmdefs 和 analyzer 加载的显式故障排查文档

### 6. Unity Editor 和可视化调试需要对当前版本建立信心

已有公开证据表明，较新 Unity 版本附近存在编辑器损坏问题。

问题：

- 可视化调试是 Entitas 在 Unity 中价值主张的重要部分
- 编辑器回归会让整个 release 即使 runtime/generator 已经稳定，看起来仍像未完成

目标状态：

- 明确的受支持 Unity 版本矩阵
- 编辑器 smoke test，或至少可重复的手工验证流程
- 对 inspector/debug tooling 具备清晰的维护者级信心

## 推荐路线图

这份路线图是围绕 release phase 组织的，而不是围绕随机修补。

## Phase 1：稳定新默认路径

目标：让增量生成器路径在本仓库内成为清晰可用的默认路径。

### 工作项

1. 移除当前仓库中的偶发摩擦点。
2. 完成内部文档向新路径的转换。
3. 审计所有仍指向旧生成器 attributes 的项目引用。
4. 验证完整 solution 在全新 checkout 下可以干净 build/test。
5. 记录 Rider 和 Unity 的受支持本地开发工作流。

### 完成定义

- 仓库无需隐藏的手工 setup 就可构建
- 当前文档与当前代码一致
- 仓库内部对哪条生成器路径是主路径没有歧义

## Phase 2：让增量生成器达到生产可用

目标：移除 `gen/Entitas.CodeGeneration` 中的原型期假设。

### 工作项

1. 用配置替换硬编码的程序集名 gating。
2. 决定配置机制。
   - 候选：`.editorconfig`
   - 候选：MSBuild properties
   - 候选：生成器专用 analyzer config options
3. 定义以下场景的预期行为：
   - `Assembly-CSharp`
   - 一个自定义 asmdef
   - 多个 asmdef
4. 扩展测试，覆盖自定义程序集场景。
5. 审查当前 attribute surface，移除或正式化过渡期残留。

### 完成定义

- 自定义 asmdef 支持是有意设计的，而不是偶然可用
- 生成器行为可配置且已文档化
- 测试证明了受支持的程序集场景

## Phase 3：迁移 Unity 示例与 canonical 用法

目标：让示例项目真正教授 2.0 的实际工作流。

### 工作项

1. 将 `samples/Unity` 从 `Entitas.Generators.Attributes` 迁移到 `Entitas.CodeGeneration.Attributes`。
2. 用新的主流程替换旧的 context 初始化模式。
3. 更新示例 systems/controllers，使其使用当前生成出的入口 API。
4. 验证示例作为学习资源在内部是自洽的。
5. 如有必要，为示例增加最小 README。

### 完成定义

- 示例代码反映真实的 2.0 用法
- 新用户可以通过示例学习，而不会混用新旧 API

## Phase 4：完成 Unity Package 方案

目标：让 Entitas 2.0 能以 Unity-first 的方式安装。

### 工作项

1. 决定 package 布局策略。
2. 定义 runtime、editor、analyzer 产物如何分发。
3. 决定 generator DLL 是在仓库内构建、作为 release artifact 分发，还是两者都做。
4. 为 package 消费者编写 Unity 安装步骤文档。
5. 在一个全新的 Unity 项目中验证 package 用法。

### 完成定义

- Unity 开发者无需反向推导仓库结构，就能安装 Entitas 2.0
- package/analyzer setup 已文档化且可重复

## Phase 5：发布一份真正的迁移指南

目标：帮助 Entitas 1 用户迁移到 2.0，而不是把它当成另一个全新框架。

### 工作项

1. 替换或覆盖 `EntitasUpgradeGuide.md`。
2. 记录 Jenny -> incremental generation 的变化。
3. 记录命名空间变化。
4. 记录生成 API 命名变化。
5. 记录 context 定义方式变化。
6. 记录 entity index、event system、cleanup system 的变化。
7. 增加常见迁移错误的故障排查。

### 完成定义

- 一个真实的 Entitas 1 项目维护者可以有信心地规划迁移

## Phase 6：加固 Unity Editor 与可视化调试

目标：让 Unity 面向用户的工具在当前受支持版本上值得信赖。

### 工作项

1. 审计已知的公开编辑器回归问题。
2. 在受支持 Unity 版本上验证可视化调试。
3. 修复 editor 代码中残留的 legacy 依赖。
4. 定义什么是 officially supported，什么只是 best effort。
5. 为未来变更加入 smoke 验证指引。

### 完成定义

- 在受支持 Unity 版本上，可视化调试不再被视为实验性功能

## Phase 7：移除或隔离 Legacy 生成器路径

目标：停止同时交付两套冲突的心智模型。

### 工作项

1. 决定 `gen/Entitas.Generators` 是继续支持、弃用，还是移除。
2. 决定 `src/Entitas.Generators.Attributes` 是继续支持、弃用，还是移除。
3. 如果保留 legacy 支持：
   - 明确隔离
   - 文档化为 legacy
   - 让 README 和 samples 避免把它当主路径
4. 如果移除 legacy 支持：
   - 删除无效引用
   - 替换或移除过时测试和文档

### 完成定义

- 用户不会再对 canonical generation path 感到困惑

## Phase 8：发布管理

目标：发布一个可信的 2.0 版本，而不是永远停留在 beta 分支。

### 工作项

1. 定义发布里程碑：
   - `2.0-beta stabilization`
   - `2.0-rc1`
   - `2.0`
2. 定义 runtime、attributes、generator 和 Unity packages 的版本策略。
3. 增加 release checklist。
4. 发布以 Unity 用户为核心的 release notes。
5. 将迁移指引与发布产物一起发布。

### 完成定义

- 项目具有从 beta 到正式发布的显式路径
- 发布标准是写下来的，而不是隐含的

## 建议的 Beta 退出标准

在满足以下条件之前，Entitas 不应离开 beta：

1. 增量生成器是文档中描述的默认路径。
2. 自定义 asmdef 支持可配置且有测试覆盖。
3. `samples/Unity` 使用 2.0 工作流。
4. Unity-first 安装文档完整。
5. 存在从 Entitas 1 迁移到 2 的指南。
6. 可视化调试/编辑器工具在受支持 Unity 版本上已验证。
7. legacy generator 的歧义已解决。
8. release artifacts 和 package 方案已敲定。

## 当前最优先的下一步

如果进展必须增量推进，按顺序应优先做这些事情：

1. 完成内部向新生成器路径的迁移。
2. 让生成器程序集选择可配置。
3. 迁移 `samples/Unity`。
4. 用 1 -> 2 迁移指南替换旧升级指南。
5. 定义 Unity package/analyzer 的交付模型。

## 未来会话的工作原则

这个 fork 后续工作应遵循以下规则：

1. 优先完成 2.0 路径，而不是增加新的旁支系统。
2. 优先一个 canonical workflow，而不是支持多个重叠 workflow。
3. 优先 Unity-first 的文档和验证。
4. 将 asmdef 支持、sample 迁移和 package 交付视为核心 release 工作，而不是可选清理项。
5. 不要让兼容性 shim 在没有明确发布理由的情况下变成永久架构。

## 推荐的会话检查清单

未来继续工作时，请检查：

1. 当前任务属于哪一个 roadmap phase。
2. 这个任务是否减少了新旧生成器之间的歧义。
3. 代码变更后是否也需要同步更新文档或 sample。
4. 这个变更是否影响 Unity package/analyzer 行为。
5. 是否发现了新的 release blocker，需要补充到这里。

## 维护说明

这份路线图应作为持续维护的项目记忆文件。

当出现重要新发现时，应更新本文档，而不是依赖对话历史。

如果优先级变化，请保留原始愿景部分，并新增 fork-specific strategy section，而不是重写历史。
