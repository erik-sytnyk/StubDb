using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StubDb;

namespace StabDbTests
{
    [TestClass]
    public class SelfReferencingModelsTest
    {
        #region Nested classes

        public class Question
        {
            public int Id { get; set; }
            public string Text { get; set; }
            public Question DependantQuestion { get; set; }

        }

        public class TestContext : StubContext
        {
            public StubSet<Question> Questions { get; set; }
        }

        #endregion

        [TestMethod]
        public void should_support_self_referencing_models()
        {
            var context = new TestContext();

            var childChildQuestion = new Question() {Text = "Child-child question"};
            var childQuestion = new Question() { Text = "Child question", DependantQuestion = childChildQuestion};
            var parentQuestion = new Question() {Text = "Parent question", DependantQuestion = childQuestion};

            context.Add(parentQuestion);

            var childFromContext = context.Questions.Query().Single(x => x.Text == "Child question");
            
            Assert.IsNotNull(childFromContext.DependantQuestion);
            Assert.AreEqual(childFromContext.DependantQuestion.Text, "Child-child question");
        }
    }
}
