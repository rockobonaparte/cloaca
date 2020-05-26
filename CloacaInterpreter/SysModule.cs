﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LanguageImplementation;
using LanguageImplementation.DataTypes;

namespace CloacaInterpreter
{
    public class SysModuleBuilder
    {
        private Scheduler scheduler;
        public SysModuleBuilder(Scheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        // I wanted to keep these private but couldn't bind them with GetMethod. I even tried using the NonPublic binding but nope.
        public ScheduledTaskRecord[] getTasksActive(PyModule module)
        {
            return scheduler.GetTasksActive();
        }

        public ScheduledTaskRecord[] getTasksBlocked(PyModule module)
        {
            return scheduler.GetTasksBlocked();
        }

        public ScheduledTaskRecord[] getTasksUnblocked(PyModule module)
        {
            return scheduler.GetTasksUnblocked();
        }

        public ScheduledTaskRecord[] getTasksYielded(PyModule module)
        {
            return scheduler.GetTasksYielded();
        }

        // [TODO][SYS.SCHEDULE - RETURN TASK] sys.schedule should return the task record or a similar handle that the caller can manage
        public void schedule(PyModule module, FrameContext context, IPyCallable call, params object[] args)
        {
            var callCodeObject = call as CodeObject;
            // [TODO][SYS.SCHEDULE - RETURN TASK - CODEOBJECT]
            if (callCodeObject != null)
            {
                if (args == null)
                {
                    scheduler.Schedule(callCodeObject, context);
                }
                else
                {
                    scheduler.Schedule(callCodeObject, context, args);
                }
            }
            // [TODO][SYS.SCHEDULE - RETURN TASK - PYCALLABLE]
            else
            {
                call.Call(scheduler.Interpreter, context, args);
            }
        }

        public PyModule CreateModule()
        {
            var sysHandle = PyModule.Create("sys");
            var schedulerHandle = PyModule.Create("scheduler");

            var me = this.GetType();
            var getActive = new WrappedCodeObject("get_active", me.GetMethod("getTasksActive"), this);
            var getBlocked = new WrappedCodeObject("get_blocked", me.GetMethod("getTasksBlocked"), this);
            var getUnblocked = new WrappedCodeObject("get_unblocked", me.GetMethod("getTasksUnblocked"), this);
            var getYielded = new WrappedCodeObject("get_yielded", me.GetMethod("getTasksYielded"), this);
            var schedule = new WrappedCodeObject("schedule", me.GetMethod("schedule"), this);

            schedulerHandle.__dict__.Add(getActive.Name, getActive);
            schedulerHandle.__dict__.Add(getBlocked.Name, getBlocked);
            schedulerHandle.__dict__.Add(getUnblocked.Name, getUnblocked);
            schedulerHandle.__dict__.Add(getYielded.Name, getYielded);
            schedulerHandle.__dict__.Add(schedule.Name, schedule);

            sysHandle.__dict__.Add("scheduler", schedulerHandle);

            return sysHandle;
        }
    }
}
