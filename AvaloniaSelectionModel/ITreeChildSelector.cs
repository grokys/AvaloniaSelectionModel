using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive.Linq;

#nullable enable

namespace Avalonia.Controls.Selection
{
    public interface ITreeChildSelector<T>
    {
        IEnumerable<T>? GetChildren(T node);
        IObservable<IEnumerable<T>?> GetChildrenObservable(T node);
    }

    internal class InpcTreeChildSelector<T> : ITreeChildSelector<T>
    {
        private readonly Func<T, IEnumerable<T>?> _selector;

        public InpcTreeChildSelector(Expression<Func<T, IEnumerable<T>?>> selector)
        {
            selector = selector ?? throw new ArgumentNullException(nameof(selector));
            _selector = selector.Compile();
        }

        public IEnumerable<T>? GetChildren(T node)
        {
            return _selector(node);
        }

        public IObservable<IEnumerable<T>?> GetChildrenObservable(T node)
        {
            return Observable.Return(_selector(node));
        }
    }
}
