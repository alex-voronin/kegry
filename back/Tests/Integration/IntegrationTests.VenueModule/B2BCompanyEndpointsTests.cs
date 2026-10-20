using System.Net;
using System.Net.Http.Json;
using IntegrationTests.Shared;
using IntegrationTests.Shared.Extensions;
using SecurityModule.Domain.ValueObjects;
using SharedCore.Api.Dto;
using SharedCore.Exceptions;
using Shouldly;
using VenueModule.API;
using VenueModule.API.B2B.Companies.Dto;

namespace IntegrationTests.VenueModule;

[Collection("IntegrationTests")]
public class B2BCompanyEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    public B2BCompanyEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenAuthenticatedB2BUser_WhenUserCreatesCompany_ThenCompanyCreated()
    {
        // 1. Sign in as B2B user
        var authResponse =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_create@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Create company
        var createRequest = new CreateCompanyRequest(Name: "Test Company", Email: "company@test.com");
        var createResponse = await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, createRequest);

        var companyId = createResponse.Result.Id;

        // 3. Get company and verify
        var getResponse = await Factory.ApiClient.GetAsync<ApiResult<GetCompanyResponse>>(
            ApiRoutes.B2B.Company.Single(companyId));

        getResponse.Result.Company.ShouldSatisfyAllConditions(
            c => c.Id.ShouldBe(companyId.ToString()),
            c => c.Name.ShouldBe(createRequest.Name),
            c => c.Email.ShouldBe(createRequest.Email)
        );
    }

    [Fact]
    public async Task GivenAuthenticatedB2BUser_WhenUserCreatesTooManyCompanies_ThenErrorReturned()
    {
        // 1. Sign in as B2B user
        var authResponse =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_create@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Create companies until we fail
        for (int i = 0; i <= Validation.MaxCompaniesPerUser; i++)
        {
            var createRequest = new CreateCompanyRequest(Name: $"Test Company {i}", Email: $"company{i}@test.com");
            if (i < Validation.MaxCompaniesPerUser)
            {
                var createResponse =
                    await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
                        ApiRoutes.B2B.Company.Root, createRequest);
                createResponse.Result.Id.ShouldNotBe(Guid.Empty);
            }
            else
            {
                var error = await Factory.ApiClient.PostProblemAsync(
                    ApiRoutes.B2B.Company.Root, createRequest);

                error.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
                error.ProblemDetails.Status.ShouldBe((int)HttpStatusCode.Forbidden);
                error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.TooManyEntities));
            }
        }
    }

    [Fact]
    public async Task GivenExistingCompany_WhenUserUpdatesCompany_ThenCompanyUpdated()
    {
        // 1. Sign in as B2B user and create company
        var authResponse =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_update@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var createRequest = new CreateCompanyRequest(Name: "Original Company", Email: "original@test.com");
        var createResponse = await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, createRequest);
        var companyId = createResponse.Result.Id;

        // 2. Update company
        var patchRequest = new PatchCompanyRequest(
            Name: "Updated Company",
            Email: "updated@test.com",
            AvatarImageUrl: TestConfig.S3.PublicUrl + "/company_avatar.jpg");

        await Factory.ApiClient.PatchAsync<PatchCompanyRequest, ApiResult.EmptyResult>(
            ApiRoutes.B2B.Company.Single(companyId), patchRequest);

        // 3. Get company and verify updates
        var getResponse = await Factory.ApiClient.GetAsync<ApiResult<GetCompanyResponse>>(
            ApiRoutes.B2B.Company.Single(companyId));

        getResponse.Result.Company.ShouldSatisfyAllConditions(
            c => c.Name.ShouldBe(patchRequest.Name),
            c => c.Email.ShouldBe(patchRequest.Email),
            c => c.AvatarImageUrl!.ShouldBeNewerOrTheSameS3Url(patchRequest.AvatarImageUrl!)
        );
    }

    [Fact]
    public async Task GivenExistingCompany_WhenUserDeletesCompany_ThenCompanyDeleted()
    {
        // 1. Sign in as B2B user and create company
        var authResponse =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_delete@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        var createRequest = new CreateCompanyRequest(Name: "To Delete", Email: "delete@test.com");
        var createResponse = await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, createRequest);
        var companyId = createResponse.Result.Id;

        // 2. Delete company
        await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(
            ApiRoutes.B2B.Company.Single(companyId));

        // 3. Verify company is not found
        var getError = await Factory.ApiClient.GetProblemAsync(
            ApiRoutes.B2B.Company.Single(companyId));

        getError.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        getError.ProblemDetails.Status.ShouldBe((int)HttpStatusCode.NotFound);
        getError.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotFound));
    }

    [Fact]
    public async Task GivenAuthenticatedB2BUser_WhenUserCreatesMultipleCompanies_ThenCompaniesReturnedInList()
    {
        // 1. Sign in as B2B user
        var authResponse =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2b_list@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Create 2 companies
        var firstRequest = new CreateCompanyRequest(Name: "Company 1", Email: "c1@test.com");
        await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, firstRequest);

        var secondRequest = new CreateCompanyRequest(Name: "Company 2", Email: "c2@test.com");
        await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root, secondRequest);

        // 3. Get companies list and verify both are present
        var listResponse = await Factory.ApiClient.GetAsync<ApiResult<GetCompaniesResponse>>(
            ApiRoutes.B2B.Company.Root);

        listResponse.Result.Companies.Length.ShouldBeGreaterThanOrEqualTo(2);
        listResponse.Result.Companies.ShouldContain(c => c.Name == firstRequest.Name);
        listResponse.Result.Companies.ShouldContain(c => c.Name == secondRequest.Name);
    }

    [Fact]
    public async Task GivenAuthenticatedB2CUser_WhenUserCreatesCompany_ThenReturnsForbidden()
    {
        // 1. Sign in as B2C user
        var authResponse =
            await Factory.AuthManager.CreateAndLoginUserAsync("b2c_intruder@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Try to create company
        var createRequest = new CreateCompanyRequest(Name: "I am B2C", Email: "b2c@test.com");
        var response = await Factory.HttpClient.PostAsJsonAsync(ApiRoutes.B2B.Company.Root, createRequest);

        // 3. Verify forbidden
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Forbidden);
    }
}