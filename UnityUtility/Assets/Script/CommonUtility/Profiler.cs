using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CommonUtility
{
    /// <summary>
    /// 性能分析器类，用于精确测量和统计代码执行时间
    /// 支持层级嵌套的性能分析，可统计执行次数、总耗时和平均耗时等详细信息
    /// 采用单例模式的全局计时器，确保时间测量的准确性
    /// 主要用于构建过程的性能监控和优化分析
    /// </summary>
    public class Profiler
    {
        /// <summary>
        /// 全局高精度计时器实例
        /// 使用静态初始化确保在整个应用程序生命周期内保持运行
        /// 提供纳秒级别的精度，用于获取准确的时间戳
        /// </summary>
        private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        
        /// <summary>
        /// 可重用的字符串构建器实例
        /// 避免频繁创建StringBuilder对象，提高性能
        /// 专门用于格式化性能分析报告的输出结果
        /// </summary>
        private static readonly StringBuilder _sb = new StringBuilder();
        
        /// <summary>
        /// 遍历栈，用于非递归方式遍历层级结构的Profiler对象
        /// 避免递归调用可能导致的栈溢出问题
        /// 实现深度优先遍历算法，确保正确的输出顺序
        /// </summary>
        private static readonly List<Profiler> _stack = new List<Profiler>();
        
        /// <summary>
        /// 子级性能分析器列表
        /// 按添加顺序存储子分析器，支持构建复杂的性能分析树结构
        /// 采用延迟初始化策略，仅在需要时创建列表对象
        /// </summary>
        private List<Profiler> _children;
        
        /// <summary>
        /// 性能分析器的唯一标识名称
        /// 用于区分不同的分析器实例，在输出报告中显示
        /// 建议使用有意义的名称，如方法名或功能描述
        /// </summary>
        private string _name;
        
        /// <summary>
        /// 层级深度信息
        /// 表示当前分析器在性能分析树中的嵌套层级
        /// 用于格式化输出时的缩进和可视化展示
        /// 根节点层级为0，每深入一层递增1
        /// </summary>
        private int _level;
        
        /// <summary>
        /// 开始时间戳（单位：ticks）
        /// 记录Start()方法调用时的精确时间
        /// 值为-1时表示分析器未启动或已停止
        /// 用于计算执行耗时：Stop时间戳 - Start时间戳
        /// </summary>
        public long _timestamp;
        
        /// <summary>
        /// 累计执行时间（单位：ticks）
        /// 记录该分析器所有执行周期的总耗时
        /// 每次调用Stop()方法时累加当前执行周期的耗时
        /// 可用于计算平均执行时间和性能趋势分析
        /// </summary>
        private long _time;
        
        /// <summary>
        /// 执行次数统计计数器
        /// 记录该分析器被启动和停止的总次数
        /// 每次成功调用Stop()方法时递增
        /// 结合累计时间可用于计算平均执行时间
        /// </summary>
        private int _count;

        /// <summary>
        /// 构造函数，创建指定名称的基础性能分析器
        /// 初始化默认状态：未启动、0次执行、0累计时间
        /// 层级深度默认为0，适用于根级分析器
        /// </summary>
        /// <param name="name">分析器的唯一标识名称，建议使用描述性的字符串</param>
        public Profiler(string name)
        {
            _name = name;
            _children = null;
            _level = 0;
            _timestamp = -1;
            _time = 0;
            _count = 0;
        }

        /// <summary>
        /// 构造函数，创建指定名称和层级深度的性能分析器
        /// 主要用于创建子级分析器，自动设置正确的层级深度
        /// 继承父级分析器的所有基本属性初始化逻辑
        /// </summary>
        /// <param name="name">分析器的唯一标识名称</param>
        /// <param name="level">层级深度，通常为父级分析器层级+1</param>
        public Profiler(string name, int level) : this(name)
        {
            _level = level;
        }

        /// <summary>
        /// 创建子级性能分析器实例
        /// 自动管理父子关系，构建性能分析树结构
        /// 子分析器的层级深度自动设置为当前分析器层级+1
        /// 支持链式调用，便于构建复杂的分析器层次结构
        /// </summary>
        /// <param name="name">子分析器的唯一标识名称</param>
        /// <returns>新创建的子级性能分析器实例</returns>
        public Profiler CreateChild(string name)
        {
            _children = _children ?? new List<Profiler>();
            var pf = new Profiler(name, _level + 1);
            _children.Add(pf);
            return pf;
        }

        /// <summary>
        /// 启动性能分析计时
        /// 记录当前时间戳作为执行开始时间
        /// 确保同一分析器实例不会重复启动，防止数据污染
        /// </summary>
        /// <exception cref="Exception">当分析器已处于启动状态时抛出异常，防止重复启动</exception>
        public void Start()
        {
            if (_timestamp != -1)
            {
                throw new Exception($"{nameof(Profiler)} {nameof(Start)} error,repeat start,name:{_name}");
            }
            _timestamp = _stopwatch.ElapsedTicks;
        }
        
        /// <summary>
        /// 停止性能分析计时并累计执行时间
        /// 计算从Start到Stop的执行耗时并累加到总时间
        /// 同时递增执行次数计数器
        /// 确保必须先调用Start才能调用Stop
        /// </summary>
        /// <exception cref="Exception">当分析器未启动时抛出异常，防止非法停止操作</exception>
        public void Stop()
        {
            if (_timestamp == -1)
            {
                throw new Exception($"{nameof(Profiler)} {nameof(Stop)} error,not start,name:{_name}");
            }
            _time += _stopwatch.ElapsedTicks - _timestamp;
            _timestamp = -1;
            _count++;
        }

        /// <summary>
        /// 格式化当前分析器的统计信息为可读字符串
        /// 按照层级结构生成可视化的树形输出格式
        /// 包含名称、执行次数、总耗时等关键性能指标
        /// 自动处理缩进和连接线，生成清晰的层次结构视图
        /// </summary>
        private void Format()
        {
            _sb.AppendLine();
            // 根据层级深度生成缩进和连接线
            for (int i = 0; i < _level; i++)
            {
                _sb.Append(i < _level - 1 ? "|  " : "|--");
            }

            // 如果没有执行记录，则不显示详细信息
            if (_count <= 0)
            {
                return;
            }
            
            // 添加分析器名称
            _sb.Append("Name");
            _sb.Append(": ");
            _sb.Append(_name);
            _sb.Append(", ");
            
            // 添加统计信息
            _sb.Append(" [");
            _sb.Append("Count");
            _sb.Append(": ");
            _sb.Append(_count);
            _sb.Append(", ");
            _sb.Append("Time");
            _sb.Append(": ");
            _sb.Append($"{(float)_time / TimeSpan.TicksPerMillisecond:F2}");
            _sb.Append("毫秒     ");
            _sb.Append($"{(float)_time / TimeSpan.TicksPerSecond:F2}");
            _sb.Append("秒     ");
            _sb.Append($"{(float)_time / TimeSpan.TicksPerMinute:F2}");
            _sb.Append("分     ");
            _sb.Append("]");
        }

        /// <summary>
        /// 重写ToString方法，返回完整的格式化性能分析报告
        /// 使用非递归的栈遍历算法处理复杂的层级结构
        /// 确保正确的深度优先遍历顺序和输出格式
        /// 报告包含完整的性能统计数据和层次结构信息
        /// </summary>
        /// <returns>完整的格式化性能分析报告字符串</returns>
        public override string ToString()
        {
            _sb.Clear();
            _stack.Clear();
            _stack.Add(this);
            
            // 使用栈进行深度优先遍历
            while (_stack.Count > 0)
            {
                int index = _stack.Count - 1;
                var pf = _stack[index];
                _stack.RemoveAt(index);
                pf.Format();
                
                // 处理子级分析器（逆序添加以保证正确的显示顺序）
                List<Profiler> children = pf._children;
                if (children == null) continue;
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    _stack.Add(children[i]);
                }
            }
            return _sb.ToString();
        }
    }
}