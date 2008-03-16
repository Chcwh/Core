﻿// Copyright 2004-2008 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.Facilities.WcfIntegration.Tests
{
	using System;
	using System.Collections.Generic;
	using System.ServiceModel;
	using System.ServiceModel.Description;
	using Castle.Core;
	using Castle.Core.Interceptor;
	using Castle.Facilities.WcfIntegration.Demo;
	using Castle.Facilities.WcfIntegration.Tests.Behaviors;
	using Castle.MicroKernel;
	using Castle.MicroKernel.Registration;
	using Castle.Windsor;
	using NUnit.Framework;

	[TestFixture]
	public class WcfServiceFixture
	{
		#region Setup/Teardown

		[SetUp]
		public void TestInitialize()
		{
			windsorContainer = new WindsorContainer()
				.AddFacility("wcf_facility", new WcfFacility())
				.Register(
					Component.For<LoggingInterceptor>(),
					Component.For<IServiceBehavior>().ImplementedBy<CallCountServiceBehavior>(),
					Component.For<IEndpointBehavior>().ImplementedBy<UnitOfworkEndPointBehavior>(),
					Component.For<IOperations>().ImplementedBy<Operations>()
						.Interceptors(InterceptorReference.ForType<LoggingInterceptor>()).Anywhere
						.CustomDependencies(new
						{
							number = 42,
							serviceModel = new WcfServiceModel()
								.AddEndpoints(
									WcfEndpoint.BoundTo(new NetTcpBinding())
										.At("net.tcp://localhost/Operations")
										)
						})
				);

			LoggingInterceptor.Calls.Clear();
			CallCountServiceBehavior.CallCount = 0;

			client = ChannelFactory<IOperations>.CreateChannel(
					new NetTcpBinding(), new EndpointAddress("net.tcp://localhost/Operations"));
		}

		[TearDown]
		public void TestCleanup()
		{
			windsorContainer.Dispose();
		}

		#endregion

		private IWindsorContainer windsorContainer;
		private IOperations client;

		[Test]
		public void CanCallServiceAndGetValueFromWindsorConfig()
		{
			int result = client.GetValueFromConstructor();
			Assert.AreEqual(42, result);
		}

		[Test]
		public void CanUseStandardDynamicProxyInterceptorsOnServices()
		{
			Assert.AreEqual(0, LoggingInterceptor.Calls.Count);
			client.GetValueFromConstructor();
			Assert.AreEqual(1, LoggingInterceptor.Calls.Count);
		}

		[Test]
		public void WillApplyEndPointBehaviors()
		{
			Assert.IsFalse(UnitOfWork.initialized, "Should be false before starting");
			bool unitOfWorkIsInitialized_DuringCall = client.UnitOfWorkIsInitialized();
			Assert.IsTrue(unitOfWorkIsInitialized_DuringCall);
			Assert.IsFalse(UnitOfWork.initialized, "Should be false after call");
		}

		[Test]
		public void WillApplyServiceBehaviors()
		{
			Assert.AreEqual(0, CallCountServiceBehavior.CallCount);
			client.GetValueFromConstructor();
			Assert.AreEqual(1, CallCountServiceBehavior.CallCount);
		}
	}

	public class LoggingInterceptor : IInterceptor
	{
		public static List<string> Calls = new List<string>();

		#region IInterceptor Members

		public void Intercept(IInvocation invocation)
		{
			Calls.Add(invocation.Method.Name);
			invocation.Proceed();
		}

		#endregion
	}
}