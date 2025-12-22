# Questions for Makis:
0. General Questions
    a. In general, it is not possible to deduce what many of the models actually represent. Recommend
         discovery session where models can be better documented.
    b. All enum-style values are strings. Can these be enumerated, or is there no consistency among hospitals. Can such fields be
         mapped to FHIR fields which do have defined values? What about case sensitivity?
    c. I have found several cases where YAML contains things that the code does not - e.g. Pli.Disposition, Pli.LDAOrders - why?

1. The concept of "Schedule" seems to be missing
2. Are hospital personnel entities even used?
3. Is "Facility" and "Department" synonymous - see DivertStatus: GetActiveDivert(departmentId) / facilitySourceId
4. What exactly is FacilitiesLocationMaster? It has no associations and seems to contain bed info (see Hierarchy)
     Similar question for UnitsLocationMaster.

