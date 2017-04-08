using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SearchPath {		// Gets starting and ending field, returns stack of subsequent fields in a path.

	public static Stack<Field> FindPath(Field Start, Field End) {
		Field start = Start;
		Field end = End;
		Stack<Field> Path = new Stack<Field>();
		List<Field> OpenList = new List<Field>();
		List<Field> ClosedList = new List<Field>();
		List<Field> adjacencies;
		Field current = start;
		OpenList.Add(start);
		while (OpenList.Count != 0 && !ClosedList.Exists(x => x.ID == end.ID)) {
			current = OpenList[0];
			OpenList.Remove(current);
			ClosedList.Add(current);
			adjacencies = current.adjactedNodes;
			foreach (Field n in adjacencies) {
				if (!ClosedList.Contains(n)) {
					if (!OpenList.Contains(n)) {
						n.Parent = current;
						n.Cost = 1 + n.Parent.Cost;
						OpenList.Add(n);
						OpenList = OpenList.OrderBy(node => node.F).ToList<Field>();
					}
				}
			}
		}
		if (!ClosedList.Exists(x => x.ID == end.ID)) {
			return null;
		}
		Field temp = ClosedList[ClosedList.IndexOf(current)];
		while (temp != start && temp != null) {
			Path.Push(temp);
			temp = temp.Parent;
		}
		return Path;
	}
}