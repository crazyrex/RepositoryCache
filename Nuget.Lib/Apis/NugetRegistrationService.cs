﻿using Ioc;
using Nuget.Repositories;
using NugetProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuget.Apis
{
    public class NugetRegistrationService : IRegistrationService, ISingleton
    {
        public NugetRegistrationService(
            IRegistrationRepository registrationRepository,
            IRegistrationPageRepository registrationPageRepository,
            IServicesMapper servicesMapper, ICatalogService catalogService)
        {
            _registrationRepository = registrationRepository;
            _registrationPageRepository = registrationPageRepository;
            _servicesMapper = servicesMapper;
            _catalogService = catalogService;
        }

        private IRegistrationRepository _registrationRepository;
        private IRegistrationPageRepository _registrationPageRepository;
        private IServicesMapper _servicesMapper;
        private ICatalogService _catalogService;

        /// <summary>
        /// Retrieve the list of registrations by id
        /// PATH: /{lowerId}/index.json
        /// The heuristic that nuget.org uses is as follows: 
        /// if there are 128 or more versions of a package, break the leaves into 
        /// pages of size 64. If there are less than 128 versions, inline all 
        /// leaves into the registration index.
        /// </summary>
        /// <param name="lowerId"></param>
        /// <param name="withSemVer2"></param>
        /// <returns></returns>
        public RegistrationIndex IndexPage(Guid repoId, string lowerId, string semVerLevel)
        {
            var resultPages = new List<RegistrationPage>();
            RegistrationPageEntity lastPage = null;
            foreach (var page in _registrationPageRepository.GetAllByPackageId(repoId, lowerId))
            {
                lastPage = page;
                resultPages.Add(new RegistrationPage(
                        _servicesMapper.FromSemver(repoId, "PackageDisplayMetadataUriTemplate", semVerLevel, lowerId, "page", page.Start, page.End + ".json"),
                        "catalog:CatalogPage",
                        page.CommitId, page.CommitTimestamp,
                        page.Count, page.Start, page.End,
                         _servicesMapper.From(repoId, "PackageDisplayMetadataUriTemplate", semVerLevel, lowerId, "index.json"),
                         null,
                         null
                        ));
            }


            var result = new RegistrationIndex(
                _servicesMapper.FromSemver(repoId, "PackageDisplayMetadataUriTemplate", semVerLevel, lowerId, "index.json"),
                new List<string> { "catalog:CatalogRoot", "PackageRegistration", "catalog:Permalink" },
                Guid.NewGuid(), DateTime.UtcNow,
                resultPages.Count, new List<RegistrationPage>(),
                new RegistrationContext(
                    _servicesMapper.From(repoId, "*Schema"), _servicesMapper.From(repoId, "*Catalog"),
                    _servicesMapper.From(repoId, "*W3SchemaComment"))
                )
            {
                Items = resultPages
            };
            if (result.Items != null && result.Items.Count == 0 && lastPage != null)
            {
                result.Items[0] = SinglePage(repoId, lowerId, lastPage.Start, lastPage.End, semVerLevel);
            }
            return result;
        }

        public RegistrationPage SinglePage(Guid repoId, string lowerId, string versionFrom, string versionTo, string semVerLevel)
        {
            var data = new List<RegistrationLeaf>();
            var maxCommitId = Guid.NewGuid();
            var lastTimestamp = DateTime.MinValue;

            foreach (var singlePage in _registrationRepository.GetRange(repoId, lowerId, versionFrom, versionTo))
            {
                if (singlePage.CommitTimestamp > lastTimestamp)
                {
                    lastTimestamp = singlePage.CommitTimestamp;
                    maxCommitId = singlePage.CommitId;
                }
                data.Add(new RegistrationLeaf(
                                _servicesMapper.From(repoId, "PackageDisplayMetadataUriTemplate", semVerLevel, lowerId, singlePage.Version + ".json"),
                                "Package",
                                singlePage.CommitId, singlePage.CommitTimestamp,
                                null,
                                _servicesMapper.From(repoId, "PackageBaseAddress/3.0.0", lowerId, singlePage.Version, lowerId + "." + singlePage.Version + ".nupkg"),
                                _servicesMapper.From(repoId, "PackageDisplayMetadataUriTemplate", semVerLevel, lowerId, "index.json"),
                                singlePage.Version
                                ));
            }

            var result = new RegistrationPage(
                        _servicesMapper.From(repoId, "PackageDisplayMetadataUriTemplate", semVerLevel, lowerId, "page", versionFrom, versionTo + ".json"),
                        "catalog:CatalogPage",
                        Guid.NewGuid(), DateTime.UtcNow,
                        data.Count, versionFrom, versionTo,
                        _servicesMapper.From(repoId, "PackageDisplayMetadataUriTemplate", semVerLevel, lowerId, "index.json"),
                        data,
                        new RegistrationContext(
                            _servicesMapper.From(repoId, "*Schema"), _servicesMapper.From(repoId, "*Catalog"),
                            _servicesMapper.From(repoId, "*W3SchemaComment")
                        )
                        );
            var versions = result.Items.Select(a => a.HiddenVersion).ToArray();
            var allPackageDetails = _catalogService.GetPackageDetailsForRegistration(repoId, lowerId, semVerLevel, versions).
                ToDictionary(a => a.Version, a => a);
            foreach (var item in result.Items)
            {
                item.CatalogEntry = allPackageDetails[item.HiddenVersion];
            }

            return result;
        }

        public RegistrationLastLeaf Leaf(Guid repoId, string lowerId, string version, string loweridVersion, string semVerLevel)
        {
            var listed = true;

            var leaf = _registrationRepository.GetSpecific(repoId, lowerId, version);
            var catalogDate = leaf.CommitTimestamp;

            var result = new RegistrationLastLeaf(
                                _servicesMapper.FromSemver(repoId, "PackageDisplayMetadataUriTemplate", semVerLevel, lowerId, version + ".json"),
                                new List<string> { "Package", _servicesMapper.From(repoId, "*CatalogPermalink") },
                                listed, leaf.CommitTimestamp,
                                _servicesMapper.From(repoId, "Catalog/3.0.0", "data", catalogDate.ToString("yyyy.MM.dd.HH.mm.ss"),
                                    lowerId + "." + version + ".json"),
                                _servicesMapper.From(repoId, "PackageBaseAddress/3.0.0", lowerId, version, lowerId + "." + version + ".nupkg"),
                                _servicesMapper.From(repoId, "PackageDisplayMetadataUriTemplate", semVerLevel, lowerId, "index.json"),
                                version,
                                new RegistrationLastLeafContext(
                                    _servicesMapper.From(repoId, "*Schema"),
                                    _servicesMapper.From(repoId, "*W3SchemaComment")
                                ));

            return result;
        }
    }
}