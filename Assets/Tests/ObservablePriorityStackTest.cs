using System.Collections.Generic;
using NUnit.Framework;
using ObservablePriorityStack;
using Assert = UnityEngine.Assertions.Assert;
using UniRx;

namespace Tests
{
    public enum DialogType
    {
        Default,
        Warning,
        Error,
    }

    public class TestDialog : PrioritizedObject<DialogType>
    {
        public int Content { get; }

        public TestDialog(int content, DialogType priorityValue) : base(priorityValue)
        {
            Content = content;
        }
    }

    public class WarningDialogTest : TestDialog
    {
        public WarningDialogTest(int content) : base(content, DialogType.Warning)
        {

        }
    }

    public class ObservablePriorityStackTest
    {

        private IEnumerable<TestDialog> BuildTestDialogList()
        {
            return new List<TestDialog>
            {
                new TestDialog(1, DialogType.Default),
                new TestDialog(2, DialogType.Error),
                new WarningDialogTest(3),
                new TestDialog(4, DialogType.Warning),
                new TestDialog(5, DialogType.Error),
                new TestDialog(6, DialogType.Default)
            };
        }

        [Test]
        public void PriorityQueueTestSimple()
        {
            var q = new ObservablePriorityStack<TestDialog>();

            var testDialogs = BuildTestDialogList();

            foreach (var testDialog in testDialogs)
            {
                q.Enqueue(testDialog);
            }

            var target = new[] { 5, 2, 4, 3, 6, 1 };
            var result = new int[6];

            var i = 0;
            while (q.Count > 0)
            {
                var d = q.Dequeue();
                result[i] = d.Content;
                i++;
            }

            string Expected()
            {
                var str = "Expected: " + target[0];
                for (int k = 1; k < target.Length; k++)
                {
                    str = str + ", " + target[k];
                }
                return str;
            }

            for (int j = 0; j < target.Length; j++)
            {

                Assert.IsTrue(result[j] == target[j], Expected());
            }
        }

        [Test]
        public void PriorityQueueCurrentObservableTest()
        {
            var q = new ObservablePriorityStack<TestDialog>();

            var testDialogs = BuildTestDialogList();

            var dialogList = new List<TestDialog>();

            var subs = q.Current.Subscribe(x =>
            {
                dialogList.Add(x);
            });

            foreach (var testDialog in testDialogs)
            {
                q.Enqueue(testDialog);
            }

            subs.Dispose();

            Assert.IsTrue(dialogList[0] ==  null);
            Assert.IsTrue(dialogList[1].Content == 1);
            Assert.IsTrue(dialogList[2].Content == 2);
            Assert.IsTrue(dialogList[3].Content == 5);
            Assert.IsTrue(dialogList.Count == 4);
        }
    }
}
