﻿using System;
using System.Collections.Generic;
using System.Linq;
using FubuCore;
using Newtonsoft.Json;
using StoryTeller.Messages;

namespace StoryTeller.Results
{
    public enum Stage
    {
        setup,
        teardown,
        before,
        after,
        timedout,
        context
    }

    public class StepResult : ClientMessage, IResultMessage
    {
        public StepResult() : base("step-result")
        {
        }

        public StepResult(string id, ResultStatus status) : this()
        {
            this.Status = status;
            this.id = id;
        }

        public StepResult(string id, Exception ex) : this(id, ResultStatus.error)
        {
            error = ex.ToDisplayMessage();
        }

        public string id { get; set; }
        public string spec { get; set; }

        public object position
        {
            get { return _position; }
            set
            {
                _position = value == null ? null : value.ToString();
            }
        }

        [JsonIgnore]
        public ResultStatus Status;

        [JsonProperty("status")]
        public string StatusDescription
        {
            get { return Status.ToString(); }
        }

        public string error;

        public CellResult[] cells = new CellResult[0];
        private object _position;

        public void Tabulate(Counts counts)
        {
            counts.Increment(Status);
            if (cells != null)
            {
                cells.Each(x => counts.Increment(x.Status));
            }
        }

        protected bool Equals(StepResult other)
        {
            return string.Equals(position, other.position) && Status == other.Status && string.Equals(error, other.error) && string.Equals(id, other.id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StepResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (position != null ? position.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (int) Status;
                hashCode = (hashCode*397) ^ (error != null ? error.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (id != null ? id.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            var description = "StepResult " + id;
            if (position != null)
            {
                description += "." + position;
            }

            description += " Status: {0}".ToFormat(Status);
            if (error.IsNotEmpty())
            {
                description += "\n  error!\n" + error;
            }

            if (cells.Any())
            {
                description += "\n  Cells:\n    *" + cells.Select(x => x.ToString()).Join("\n    * ");
            }

            return description;
        }

        public string type
        {
            get { return "step-result"; }
        }
    }
}