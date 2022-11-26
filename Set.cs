using System.Collections;

namespace ParserGen;     

internal class Set<T> : IEnumerable<T>, ICollection<T> {

    private readonly List<T> m_items;

    public T this[int index] => this.m_items[index];

    public int Count => this.m_items.Count;

    public bool IsReadOnly => ((ICollection<T>)this.m_items).IsReadOnly;

    public Set() {
        this.m_items = new(128); // Create new list with capacity of 128 elements -> we are greedy on memory in the hopes of avoiding too many resizes
    }

    public Set(Set<T> baseSet) {
        this.m_items = new(baseSet);
    }

    public Set(IEnumerable<T> collection) : this() {
        foreach (T item in collection) {
            this.Add(item);
        }
    }

    public void Clear() => this.m_items.Clear();

    public bool Add(T item) {
        for (int i = 0; i < this.m_items.Count; i++) {
            var itm = this.m_items[i];
            if (itm is not null && itm.Equals(item)) {
                return false;
            }
        }
        this.m_items.Add(item);
        return true;
    }

    public bool Add(T item, Func<T, T, bool> predicate) {
        for (int i = 0; i < this.m_items.Count; i++) {
            var itm = this.m_items[i];
            if (predicate(item, itm)) {
                return false;
            }
        }
        this.m_items.Add(item);
        return true;
    }

    public bool Remove(T item) => this.m_items.Remove(item);

    public int Union(Set<T> other) {
        int count = 0;
        for (int i = 0; i < other.m_items.Count; i++) {
            if (this.Add(other.m_items[i])) {
                count++;
            }
        }
        return count;
    }

    public bool Contains(T item, Func<T, T, bool> predicate) {
        for (int i = 0; i < this.m_items.Count; i++) {
            if (predicate(this.m_items[i], item)) {
                return true;
            }
        }
        return false;
    }

    public bool ContainsEachother(Set<T> other) {
        for (int i = 0; i < this.m_items.Count; i++) {
            if (!other.Contains(this.m_items[i], (a,b) => a?.Equals(b) ?? false)) {
                return false;
            }
        }
        for (int i = 0; i < other.m_items.Count; i++) {
            if (!this.Contains(other.m_items[i], (a, b) => a?.Equals(b) ?? false)) {
                return false;
            }
        }
        return true;
    }

    public override string ToString() => $"{nameof(this.Count)} = {this.Count}";

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)this.m_items).GetEnumerator();
    
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.m_items).GetEnumerator();

    void ICollection<T>.Add(T item) => this.Add(item);

    public bool Contains(T item) => this.m_items.Any(x => item?.Equals(x) ?? false);
    
    public void CopyTo(T[] array, int arrayIndex) => ((ICollection<T>)this.m_items).CopyTo(array, arrayIndex);

}
